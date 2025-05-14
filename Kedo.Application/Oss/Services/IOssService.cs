
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kedo.Application.Oss.Services
{
    public interface IOssService
    {
        object GetPostObjectParams(string imgAppType);

        /// <summary>
        /// 上传文件  OnionServer 
        /// </summary>
        /// <param name="formFiles"></param>
        /// <param name="conversationId"></param>
        /// <param name="tagId"></param>
        /// <returns></returns>
        List<string> UploadDocuments(List<IFormFile> formFiles, string conversationId, string tagId);

        /// <summary>
        /// 上传图片
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        string UploadImg(IFormFile formFile, string userId);
    }
}
