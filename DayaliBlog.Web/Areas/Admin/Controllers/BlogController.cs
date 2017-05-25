using System;
using System.IO;
using DayaliBlog.Model.Blog;
using DayaliBlog.Service.Blog;
using DayaliBlog.Service.Sys;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;

namespace DayaliBlog.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BlogController : Controller
    {
        readonly BlogCategService _categService = new BlogCategService();
        readonly BlogContentService _contentService = new BlogContentService();
        readonly BlogCategRelService _relCateg = new BlogCategRelService();

        //用于读取网站静态文件目录
        private IHostingEnvironment hostingEnv;
        public BlogController(IHostingEnvironment env)
        {
            hostingEnv = env;
        }
        public IActionResult GetTotalCount(string key, string start, string end, string categ)
        {
            string where = GetWhere(key, start, end, categ);
            int count = _contentService.GetCount(where);
            return Content(count.ToString());
        }

        public string GetWhere(string key, string start, string end, string categ)
        {
            string where = " 1=1 ";
            if (!string.IsNullOrEmpty(key))
            {
                where += $" and BlogContent like '%{key}%'";
            }
            if (!string.IsNullOrEmpty(start))
            {
                DateTime dtStart;
                if (DateTime.TryParse(start, out dtStart))
                    where += $" and CreateTIme>{dtStart:yyyy-MM-dd}";
            }
            if (!string.IsNullOrEmpty(end))
            {
                DateTime dtEnd;
                if (DateTime.TryParse(end, out dtEnd))
                {
                    where += $" and CreateTIme<{dtEnd:yyyy-MM-dd}";
                }
            }
            if (!string.IsNullOrEmpty(categ))
            {
                categ = Helper.GetSafeSQL(categ);
                where += $" and g.CatelogID={categ}";
            }
            return where;
        }

        public IActionResult GetListByPage(int pageIndex, int pageSize, string key, string start, string end, string categ)
        {
            var contion = GetWhere(key, start, end, categ);
            var list = _contentService.GetListByPage("", pageSize, pageIndex, contion);
            return Json(list);
        }

        public IActionResult Index()
        {
            ViewBag.CategList = _categService.GetList("");
            var list = _contentService.GetList("");
            return View(list);
        }


        public IActionResult Add(int? id)
        {
            ViewBag.BlogTypes = SysConfig.GetConfigList(SysConfig.BlogType);
            ViewBag.CategList = _categService.GetList("");
            T_BLOG_CONTENT content = new T_BLOG_CONTENT();
            if (id != null)
            {
                content = _contentService.GetModel(" b.BlogID=" + id.Value);
            }
            return View(content);
        }

        [HttpPost]
        public IActionResult Add(T_BLOG_CONTENT content)
        {
            if (!ModelState.IsValid)
                return Content("<script> alert(\'博客内容有误，请检查博客内容！\'); location.href=\'/Admin/Login\'</script>\", \"text/html");
            int blogId = 0;
            if (content.BlogID == 0)
            {
                content.CreateTIme = DateTime.Now;
                content.CreateUser = 1;
                content.LastUptTime = DateTime.Now;
                content.BlogState = 1;
                blogId = _contentService.Insert(content);
            }
            else
            {
                blogId = content.BlogID;
                content.UpdateUser = 1;
                content.LastUptTime = DateTime.Now;
                content.BlogState = 1;
                bool isSuccess = _contentService.Update(content);
                if (isSuccess)
                    _relCateg.Delete(content.BlogID);
            }
            if (blogId > 0)
                _relCateg.Insert(blogId, content.CatelogID);
            return Redirect("/Admin/Blog/Index");
        }
        [HttpPost]
        public IActionResult Del(int id)
        {
            if (_relCateg.Delete(id) && _contentService.Delete(id))
                return Content("删除成功！");
            return Content("删除失败！");
        }
        /// <summary>
        /// layui编辑器里的上传图片功能 
        //{
        //  "code": 0 //0表示成功，其它失败
        //  ,"msg": "" //提示信息 //一般上传失败后返回
        //  ,"data": {
        //    "src": "图片路径"
        //    ,"title": "图片名称" //可选
        //  }
        //}
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ImgUpload()
        {
            var imgFile = Request.Form.Files[0];
            if (imgFile != null && !string.IsNullOrEmpty(imgFile.FileName))
            {
                long size = 0;
                string tempname = "";
                var filename = ContentDispositionHeaderValue
                    .Parse(imgFile.ContentDisposition)
                    .FileName
                    .Trim('"');
                var extname = filename.Substring(filename.LastIndexOf("."), filename.Length - filename.LastIndexOf(".")); //扩展名，如.jpg

                #region 判断后缀
                if (!extname.ToLower().Contains("jpg") && !extname.ToLower().Contains("png") && !extname.ToLower().Contains("gif"))
                {
                    return Json(new { code = 1, msg = "只允许上传jpg,png,gif格式的图片.", });
                }
                #endregion

                #region 判断大小
                long mb = imgFile.Length / 1024 / 1024; // MB
                if (mb > 5)
                {
                    return Json(new { code = 1, msg = "只允许上传小于 5MB 的图片.", });
                }
                #endregion

                var filename1 = System.Guid.NewGuid().ToString().Substring(0, 6) + extname;
                tempname = filename1;
                var path = hostingEnv.WebRootPath; //网站静态文件目录  wwwroot
                string dir = DateTime.Now.ToString("yyyyMMdd");
                //完整物理路径
                string wuli_path = hostingEnv.WebRootPath + $"{Path.DirectorySeparatorChar}upload{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}";
                if (!System.IO.Directory.Exists(wuli_path))
                {
                    System.IO.Directory.CreateDirectory(wuli_path);
                }
                filename = wuli_path + filename1;
                size += imgFile.Length;
                using (FileStream fs = System.IO.File.Create(filename))
                {
                    imgFile.CopyTo(fs);
                    fs.Flush();
                }
                return Json(new { code = 0, msg = "上传成功", data = new { src = $"/upload/{dir}/{filename1}", title = filename1 } });
            }
            return Json(new { code = 1, msg = "上传失败", });
        }

    }
}