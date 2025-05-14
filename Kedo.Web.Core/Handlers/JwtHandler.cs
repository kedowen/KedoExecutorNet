using Furion.Authorization;
using Furion.DataEncryption;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;
using Azure.Core;

namespace Kedo.Web.Core;



public class JwtHandler : AppAuthorizeHandler
{

    /// <summary>
    /// 重写 Handler 添加自动刷新授权逻辑
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    // public override async Task HandleAsync(AuthorizationHandlerContext context)  // Furion 4.9.3 之前版本使用这个 当前 "Furion" Version="3.2.1"
    // public override async Task HandleAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    public override async Task HandleAsync(AuthorizationHandlerContext context)
    {
        // 自动刷新 token
        if (JWTEncryption2.AutoRefreshToken(context, context.GetCurrentHttpContext()))
        {
            await AuthorizeHandleAsync(context);
        }
        else context.Fail();    // 授权失败
    }
    public override Task<bool> PipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        // 这里写您的授权判断逻辑，授权通过返回 true，否则返回 false
        return Task.FromResult(true);

    }


}

public class JWTEncryption2
{
    //
    // 摘要:
    //     日期类型的 Claim 类型
    private static readonly string[] DateTypeClaimTypes = new string[3] { "iat", "nbf", "exp" };

    //
    // 摘要:
    //     框架 App 静态类
    internal static Type FrameworkApp { get; set; }

    //
    // 摘要:
    //     生成 Token
    //
    // 参数:
    //   payload:
    //
    //   expiredTime:
    //     过期时间（分钟）
    public static string Encrypt(IDictionary<string, object> payload, long? expiredTime = null)
    {
        var (payload2, jWTSettingsOptions) = CombinePayload(payload, expiredTime);
        return Encrypt(jWTSettingsOptions.IssuerSigningKey, payload2, jWTSettingsOptions.Algorithm);
    }

    //
    // 摘要:
    //     生成 Token
    //
    // 参数:
    //   issuerSigningKey:
    //
    //   payload:
    //
    //   algorithm:
    public static string Encrypt(string issuerSigningKey, IDictionary<string, object> payload, string algorithm = "HS256")
    {
        string payload2 = ((payload is JwtPayload jwtPayload) ? jwtPayload.SerializeToJson() : JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }));
        return Encrypt(issuerSigningKey, payload2, algorithm);
    }

    //
    // 摘要:
    //     生成 Token
    //
    // 参数:
    //   issuerSigningKey:
    //
    //   payload:
    //
    //   algorithm:
    public static string Encrypt(string issuerSigningKey, string payload, string algorithm = "HS256")
    {
        SigningCredentials signingCredentials = null;
        if (!string.IsNullOrWhiteSpace(issuerSigningKey))
        {
            signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(issuerSigningKey)), algorithm);
        }

        JsonWebTokenHandler jsonWebTokenHandler = new JsonWebTokenHandler();
        if (signingCredentials != null)
        {
            return jsonWebTokenHandler.CreateToken(payload, signingCredentials);
        }

        return jsonWebTokenHandler.CreateToken(payload);
    }

    //
    // 摘要:
    //     生成刷新 Token
    //
    // 参数:
    //   accessToken:
    //
    //   expiredTime:
    //     刷新 Token 有效期（分钟）
    public static string GenerateRefreshToken(string accessToken, int expiredTime = 43200)
    {
        string[] array = accessToken.Split('.', StringSplitOptions.RemoveEmptyEntries);
        int @int = RandomNumberGenerator.GetInt32(10, array[1].Length / 2 + 2);
        int int2 = RandomNumberGenerator.GetInt32(3, 13);
        return Encrypt(new Dictionary<string, object>
        {
            {
                "f",
                array[0]
            },
            {
                "e",
                array[2]
            },
            { "s", @int },
            { "l", int2 },
            {
                "k",
                array[1].Substring(@int, int2)
            }
        }, expiredTime);
    }

    //
    // 摘要:
    //     通过过期Token 和 刷新Token 换取新的 Token
    //
    // 参数:
    //   expiredToken:
    //
    //   refreshToken:
    //
    //   expiredTime:
    //     过期时间（分钟）
    //
    //   clockSkew:
    //     刷新token容差值，秒做单位
    public static string Exchange(string expiredToken, string refreshToken, long? expiredTime = null, long clockSkew = 500L)
    {

        clockSkew = 500;

        if (Validate(expiredToken).IsValid)
        {
            return null;
        }

        var (flag, jsonWebToken, _) = Validate(refreshToken);
        if (!flag)
        {
            return null;
        }

        HttpContext currentHttpContext = GetCurrentHttpContext();
        string key = "BLACKLIST_REFRESH_TOKEN:" + refreshToken;
        IDistributedCache distributedCache = currentHttpContext?.RequestServices?.GetService<IDistributedCache>();
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        string text = distributedCache?.GetString(key);
        bool flag2 = !string.IsNullOrWhiteSpace(text);
        if (flag2)
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(long.Parse(text), TimeSpan.Zero);
            if ((utcNow - dateTimeOffset).TotalSeconds > (double)clockSkew)
            {
                return null;
            }
        }

        string[] array = expiredToken.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (array.Length < 3)
        {
            return null;
        }

        if (!jsonWebToken.GetPayloadValue<string>("f").Equals(array[0]))
        {
            return null;
        }

        if (!jsonWebToken.GetPayloadValue<string>("e").Equals(array[2]))
        {
            return null;
        }

        if (!array[1].Substring(jsonWebToken.GetPayloadValue<int>("s"), jsonWebToken.GetPayloadValue<int>("l")).Equals(jsonWebToken.GetPayloadValue<string>("k")))
        {
            return null;
        }

        JwtPayload payload = SecurityReadJwtToken(expiredToken).Payload;
        string[] dateTypeClaimTypes = DateTypeClaimTypes;
        foreach (string key2 in dateTypeClaimTypes)
        {
            if (payload.ContainsKey(key2))
            {
                payload.Remove(key2);
            }
        }

        if (!flag2)
        {
            distributedCache?.SetString(key, utcNow.Ticks.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.FromUnixTimeSeconds(jsonWebToken.GetPayloadValue<long>("exp"))
            });
        }

        return Encrypt(payload, expiredTime);
    }

    //
    // 摘要:
    //     自动刷新 Token 信息
    //
    // 参数:
    //   context:
    //
    //   httpContext:
    //
    //   expiredTime:
    //     新 Token 过期时间（分钟）
    //
    //   refreshTokenExpiredTime:
    //     新刷新 Token 有效期（分钟）
    //
    //   tokenPrefix:
    //
    //   clockSkew:
    public static bool AutoRefreshToken(AuthorizationHandlerContext context, DefaultHttpContext httpContext, long? expiredTime = null, int refreshTokenExpiredTime = 43200, string tokenPrefix = "Bearer ", long clockSkew = 5L)
    {
        if (context.User.Identity.IsAuthenticated)
        {
            return true;
        }

        if (httpContext.GetEndpoint()?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            return true;
        }

        string jwtBearerToken = GetJwtBearerToken(httpContext, "Authorization", tokenPrefix);
        string jwtBearerToken2 = GetJwtBearerToken(httpContext, "X-Authorization", tokenPrefix);




        if (string.IsNullOrWhiteSpace(jwtBearerToken) || string.IsNullOrWhiteSpace(jwtBearerToken2))
        {
            return false;
        }

        string text = Exchange(jwtBearerToken, jwtBearerToken2, expiredTime, clockSkew);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        IEnumerable<Claim> enumerable = ReadJwtToken(text)?.Claims;
        if (enumerable == null)
        {
            return false;
        }

        ClaimsIdentity claimsIdentity = new ClaimsIdentity("AuthenticationTypes.Federation");
        claimsIdentity.AddClaims(enumerable);
        ClaimsPrincipal principal = (httpContext.User = new ClaimsPrincipal(claimsIdentity));
        httpContext.SignInAsync(principal);
        string text2 = "access-token";
        string text3 = "x-access-token";
        string key = "Access-Control-Expose-Headers";


       string  refreshToken= GenerateRefreshToken(text, refreshTokenExpiredTime);

        httpContext.Response.Headers[text2] = text;
        httpContext.Response.Headers[text3] = refreshToken;

        httpContext.Response.Headers["Authorization"] = text;
        httpContext.Response.Headers["X-Authorization"] = refreshToken;


        //-----new-----------

        //httpContext.Response.Headers.Add("Authorization", $"Bearer {text}");
        //httpContext.Response.Headers.Add("X-Authorization", $"Bearer {refreshToken}");


        //httpContext.Response.Headers["access-token"] = text;
        //httpContext.Response.Headers["x-access-token"] = refreshToken;

        //----------------------------

        httpContext.Response.Headers.TryGetValue(key, out var value);
        httpContext.Response.Headers[key] = string.Join(',', StringValues.Concat(value, new StringValues(new string[2] { text2, text3 })).Distinct());
        return true;
    }

    //
    // 摘要:
    //     验证 Token
    //
    // 参数:
    //   accessToken:
    public static (bool IsValid, JsonWebToken Token, TokenValidationResult validationResult) Validate(string accessToken)
    {
        JWTSettingsOptions jWTSettings = GetJWTSettings();
        if (jWTSettings == null)
        {
            return (false, null, null);
        }

        SigningCredentials signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jWTSettings.IssuerSigningKey)), jWTSettings.Algorithm);
        TokenValidationParameters tokenValidationParameters = CreateTokenValidationParameters(jWTSettings);
        TokenValidationParameters tokenValidationParameters2 = tokenValidationParameters;
        if (tokenValidationParameters2.IssuerSigningKey == null)
        {
            SecurityKey securityKey = (tokenValidationParameters2.IssuerSigningKey = signingCredentials.Key);
        }

        JsonWebTokenHandler jsonWebTokenHandler = new JsonWebTokenHandler();
        try
        {
            TokenValidationResult tokenValidationResult = jsonWebTokenHandler.ValidateToken(accessToken, tokenValidationParameters);
            if (!tokenValidationResult.IsValid)
            {
                return (false, null, tokenValidationResult);
            }

            JsonWebToken item = tokenValidationResult.SecurityToken as JsonWebToken;
            return (true, item, tokenValidationResult);
        }
        catch
        {
            return (false, null, null);
        }
    }



    //
    // 摘要:
    //     验证 Token
    //
    // 参数:
    //   httpContext:
    //
    //   token:
    //
    //   headerKey:
    //
    //   tokenPrefix:


    //
    // 摘要:
    //     读取 Token，不含验证
    //
    // 参数:
    //   accessToken:
    public static JsonWebToken ReadJwtToken(string accessToken)
    {
        JsonWebTokenHandler jsonWebTokenHandler = new JsonWebTokenHandler();
        if (jsonWebTokenHandler.CanReadToken(accessToken))
        {
            return jsonWebTokenHandler.ReadJsonWebToken(accessToken);
        }

        return null;
    }

    //
    // 摘要:
    //     读取 Token，不含验证
    //
    // 参数:
    //   accessToken:
    public static JwtSecurityToken SecurityReadJwtToken(string accessToken)
    {
        return new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
    }

    //
    // 摘要:
    //     获取 JWT Bearer Token
    //
    // 参数:
    //   httpContext:
    //
    //   headerKey:
    //
    //   tokenPrefix:
    public static string GetJwtBearerToken(DefaultHttpContext httpContext, string headerKey = "Authorization", string tokenPrefix = "Bearer ")
    {
        string text = httpContext.Request.Headers[headerKey].ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        int length = tokenPrefix.Length;
        if (!text.StartsWith(tokenPrefix, ignoreCase: true, null) || text.Length <= length)
        {
            return null;
        }

        string text2 = text;
        int num = length;
        return text2.Substring(num, text2.Length - num);
    }

    //
    // 摘要:
    //     获取 JWT 配置
    public static JWTSettingsOptions GetJWTSettings()
    {

        _ = GetFrameworkContext(Assembly.GetCallingAssembly());

        if (FrameworkApp == null)
        {
            JWTSettingsOptions jWTSettingsOptions = new JWTSettingsOptions();
            SetDefaultJwtSettings(jWTSettingsOptions);
            return jWTSettingsOptions;
        }

        return (FrameworkApp.GetMethod("GetOptions").MakeGenericMethod(typeof(JWTSettingsOptions)).Invoke(null, new object[1]) as JWTSettingsOptions) ?? SetDefaultJwtSettings(new JWTSettingsOptions());
    }

    //
    // 摘要:
    //     生成Token验证参数
    //
    // 参数:
    //   jwtSettings:
    public static TokenValidationParameters CreateTokenValidationParameters(JWTSettingsOptions jwtSettings)
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey.Value,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.IssuerSigningKey)),
            ValidateIssuer = jwtSettings.ValidateIssuer.Value,
            ValidIssuer = jwtSettings.ValidIssuer,
            ValidateAudience = jwtSettings.ValidateAudience.Value,
            ValidAudience = jwtSettings.ValidAudience,
            ValidateLifetime = jwtSettings.ValidateLifetime.Value,
            ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkew.Value)
        };
    }

    //
    // 摘要:
    //     组合 Claims 负荷
    //
    // 参数:
    //   payload:
    //
    //   expiredTime:
    //     过期时间，单位：分钟
    private static (IDictionary<string, object> Payload, JWTSettingsOptions JWTSettings) CombinePayload(IDictionary<string, object> payload, long? expiredTime = null)
    {
        JWTSettingsOptions jWTSettings = GetJWTSettings();
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        if (!payload.ContainsKey("iat"))
        {
            payload.Add("iat", utcNow.ToUnixTimeSeconds());
        }

        if (!payload.ContainsKey("nbf"))
        {
            payload.Add("nbf", utcNow.ToUnixTimeSeconds());
        }

        if (!payload.ContainsKey("exp"))
        {
            long num = expiredTime ?? jWTSettings?.ExpiredTime ?? 20;
            payload.Add("exp", DateTimeOffset.UtcNow.AddMinutes(num).ToUnixTimeSeconds());
        }

        if (!payload.ContainsKey("iss"))
        {
            payload.Add("iss", jWTSettings?.ValidIssuer);
        }

        if (!payload.ContainsKey("aud"))
        {
            payload.Add("aud", jWTSettings?.ValidAudience);
        }

        return (payload, jWTSettings);
    }

    //
    // 摘要:
    //     设置默认 Jwt 配置
    //
    // 参数:
    //   options:
    internal static JWTSettingsOptions SetDefaultJwtSettings(JWTSettingsOptions options)
    {
        JWTSettingsOptions jWTSettingsOptions = options;
        bool? validateIssuerSigningKey = jWTSettingsOptions.ValidateIssuerSigningKey;
        bool valueOrDefault = validateIssuerSigningKey.GetValueOrDefault();
        if (!validateIssuerSigningKey.HasValue)
        {
            valueOrDefault = true;
            JWTSettingsOptions jWTSettingsOptions2 = jWTSettingsOptions;
            bool? validateIssuerSigningKey2 = valueOrDefault;
            jWTSettingsOptions2.ValidateIssuerSigningKey = validateIssuerSigningKey2;
        }

        if (options.ValidateIssuerSigningKey == true)
        {
            jWTSettingsOptions = options;
            if (jWTSettingsOptions.IssuerSigningKey == null)
            {
                string text2 = (jWTSettingsOptions.IssuerSigningKey = "U2FsdGVkX1+6H3D8Q//yQMhInzTdRZI9DbUGetbyaag=");
            }
        }

        jWTSettingsOptions = options;
        validateIssuerSigningKey = jWTSettingsOptions.ValidateIssuer;
        valueOrDefault = validateIssuerSigningKey.GetValueOrDefault();
        if (!validateIssuerSigningKey.HasValue)
        {
            valueOrDefault = true;
            JWTSettingsOptions jWTSettingsOptions3 = jWTSettingsOptions;
            bool? validateIssuerSigningKey2 = valueOrDefault;
            jWTSettingsOptions3.ValidateIssuer = validateIssuerSigningKey2;
        }

        if (options.ValidateIssuer == true)
        {
            jWTSettingsOptions = options;
            if (jWTSettingsOptions.ValidIssuer == null)
            {
                string text2 = (jWTSettingsOptions.ValidIssuer = "dotnetchina");
            }
        }

        jWTSettingsOptions = options;
        validateIssuerSigningKey = jWTSettingsOptions.ValidateAudience;
        valueOrDefault = validateIssuerSigningKey.GetValueOrDefault();
        if (!validateIssuerSigningKey.HasValue)
        {
            valueOrDefault = true;
            JWTSettingsOptions jWTSettingsOptions4 = jWTSettingsOptions;
            bool? validateIssuerSigningKey2 = valueOrDefault;
            jWTSettingsOptions4.ValidateAudience = validateIssuerSigningKey2;
        }

        if (options.ValidateAudience == true)
        {
            jWTSettingsOptions = options;
            if (jWTSettingsOptions.ValidAudience == null)
            {
                string text2 = (jWTSettingsOptions.ValidAudience = "powerby Furion");
            }
        }

        jWTSettingsOptions = options;
        validateIssuerSigningKey = jWTSettingsOptions.ValidateLifetime;
        valueOrDefault = validateIssuerSigningKey.GetValueOrDefault();
        if (!validateIssuerSigningKey.HasValue)
        {
            valueOrDefault = true;
            JWTSettingsOptions jWTSettingsOptions5 = jWTSettingsOptions;
            bool? validateIssuerSigningKey2 = valueOrDefault;
            jWTSettingsOptions5.ValidateLifetime = validateIssuerSigningKey2;
        }

        long? clockSkew;
        long valueOrDefault2;
        if (options.ValidateLifetime == true)
        {
            jWTSettingsOptions = options;
            clockSkew = jWTSettingsOptions.ClockSkew;
            valueOrDefault2 = clockSkew.GetValueOrDefault();
            if (!clockSkew.HasValue)
            {
                valueOrDefault2 = 10L;
                JWTSettingsOptions jWTSettingsOptions6 = jWTSettingsOptions;
                long? clockSkew2 = valueOrDefault2;
                jWTSettingsOptions6.ClockSkew = clockSkew2;
            }
        }

        jWTSettingsOptions = options;
        clockSkew = jWTSettingsOptions.ExpiredTime;
        valueOrDefault2 = clockSkew.GetValueOrDefault();
        if (!clockSkew.HasValue)
        {
            valueOrDefault2 = 20L;
            JWTSettingsOptions jWTSettingsOptions7 = jWTSettingsOptions;
            long? clockSkew2 = valueOrDefault2;
            jWTSettingsOptions7.ExpiredTime = clockSkew2;
        }

        jWTSettingsOptions = options;
        if (jWTSettingsOptions.Algorithm == null)
        {
            string text2 = (jWTSettingsOptions.Algorithm = "HS256");
        }

        return options;
    }

    //
    // 摘要:
    //     获取当前的 HttpContext
    private static HttpContext GetCurrentHttpContext()
    {
        return FrameworkApp.GetProperty("HttpContext").GetValue(null) as HttpContext;
    }

    //
    // 摘要:
    //     获取框架上下文
    internal static Assembly GetFrameworkContext(Assembly callAssembly)
    {
        if (FrameworkApp != null)
        {
            return FrameworkApp.Assembly;
        }

        AssemblyName assemblyName = callAssembly.GetReferencedAssemblies().FirstOrDefault((AssemblyName u) => u.Name == "Furion" || u.Name == "Furion.Pure") ?? throw new InvalidOperationException("No `Furion` assembly installed in the current project was detected.");
        Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
        FrameworkApp = assembly.GetType("Furion.App");
        return assembly;
    }
}



//public class JwtHandler : AppAuthorizeHandler
//{


//    //public override async Task HandleAsync(AuthorizationHandlerContext context)
//    //{
//    //  //  Console.WriteLine("进入jwt验证");

//    //    var currentHttpContext = context.GetCurrentHttpContext();
//    //    // 检查并自动刷新 token, (第三个参数,新token有效期设置为1分钟,便于测试)
//    //    if (JWTEncryption.AutoRefreshToken(context, currentHttpContext, 1))
//    //    {
//    //        await AuthorizeHandleAsync(context);
//    //    }
//    //    else
//    //    {
//    //        context.Fail(); // 授权失败
//    //    }
//    //}


//    public override Task<bool> PipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
//    {
//        // 这里写您的授权判断逻辑，授权通过返回 true，否则返回 false

//        return Task.FromResult(true);
//    }
//    /// <summary>
//    /// 检查权限
//    /// </summary>
//    /// <param name="httpContext"></param>
//    /// <returns></returns>
//    private static bool CheckAuthorzie(DefaultHttpContext httpContext)
//    {
//        //访问用户信息
//        var claims = httpContext.User.Claims.ToArray();
//        var user = claims.Where(it => it.Type == "UserId").FirstOrDefault();




//        return true;
//    }

//}
