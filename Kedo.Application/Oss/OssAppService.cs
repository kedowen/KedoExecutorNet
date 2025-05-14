using Furion.DynamicApiController;
using Kedo.Application.Oss.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kedo.Application.Oss
{
    public class OssAppService : IDynamicApiController
    {
        private readonly IOssService _ossService;

        public OssAppService(IOssService ossService)
        {
            _ossService = ossService;
        }

        /// <summary>
        /// 获取OSS签名   类别 : BorderImg   BackgroundImg   DataAnalysisImg   UserHeadImg  AgentImg 
        /// </summary>
        /// <param name="imgAppType">图片作用域 背景图片  边框图片</param>
        /// <returns></returns>
        [HttpGet]
        public object GetPostObjectParams([FromQuery] string imgAppType)
        {
            return _ossService.GetPostObjectParams(imgAppType);
        }

        /// <summary>
        /// 上传文件到本地服务器
        /// </summary>
        /// <param name="formFiles"></param>
        /// <param name="conversationId"></param>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public List<string> UploadDocuments(List<IFormFile> formFiles, [FromQuery] string conversationId, [FromQuery] string tagId)
        {
            return _ossService.UploadDocuments(formFiles, conversationId, tagId);
        }

        /// <summary>
        /// 上传图片
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public string UploadImg(IFormFile formFile, [FromQuery] string userId)
        {
            return _ossService.UploadImg(formFile, userId);
        }

    }
}
