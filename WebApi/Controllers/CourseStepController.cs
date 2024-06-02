using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Net;
using WebApi.DAL;
using WebApi.Model;
using WebApi.Service;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CourseStepController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<CourseStepController> _logger;

        public CourseStepController(AppDbContext context, IConfiguration config, ILogger<CourseStepController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;

        }
        // GET: api/CourseStep
        [HttpGet]
        public ActionResult<IEnumerable<CourseStepModel>> Get()
        {
            try
            {
                var courseSteps = _context.CourseStep.ToList();
                _logger.LogInformation("CourseStepController - Retrieved all course steps successfully.");
                return courseSteps;
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while retrieving course steps: {ex.Message}");                
                throw;
            }
        }

        // GET: api/CourseStep/5
        [HttpGet("{id}")]
        public ActionResult<CourseStepModel> Get(int id)
        {
            try
            {
                var courseStep = _context.CourseStep.Find(id);

                if (courseStep == null)
                {
                    _logger.LogWarning($"CourseStepController - Course step with ID {id} not found.");
                    return NotFound();
                }

                _logger.LogInformation($"CourseStepController - Retrieved course step with ID {id} successfully.");
                return courseStep;
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while retrieving course step with ID {id}: {ex.Message}");
                
                throw;
            }
        }

        // POST: api/CourseStep
        [HttpPost]
        public IActionResult Post([FromBody] CourseStepModel courseStep)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("CourseStepController - Invalid model state detected while trying to create a new course step.");
                    return BadRequest(ModelState);
                }

                _context.CourseStep.Add(courseStep);
                _context.SaveChanges();

                _logger.LogInformation($"CourseStepController - Course step with ID {courseStep.ID} created successfully.");

                return CreatedAtAction(nameof(Get), new { id = courseStep.ID }, courseStep);
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while creating course step: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpPost("fileupload")]
        [DisableRequestSizeLimit]
        public  IActionResult UploadFiles(int CourseID, int StepNo, string StepTitle, string ContentType, List<IFormFile> StepContents, IFormFile Description)
        {
            try
            {
                //var basePath = Directory.GetCurrentDirectory();
                var basePath = Path.Combine(_config["AssetFolder:AssetFolderPath"]);
                var stepFolderPath = Path.Combine(basePath, $"Course_{CourseID}", $"Step_{StepNo}");
                var filecompress = new FileUploadWithCompress();

                if (Directory.Exists(stepFolderPath))
                {
                    Directory.Delete(stepFolderPath, true);
                    _logger.LogInformation($"CourseStepController - Deleted existing folder for Step {StepNo} of Course {CourseID}.");
                }

                Directory.CreateDirectory(stepFolderPath);
                _logger.LogInformation($"CourseStepController - Created folder for Step {StepNo} of Course {CourseID}.");

                var contentFileNames = new List<string>();

                foreach (var file in StepContents)
                {
                    var contentFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var contentFilePath = Path.Combine(stepFolderPath, contentFileName);


                    if (ContentType == "Image")
                    {
                         filecompress.CompressAndSaveImageAsync(file, contentFilePath, 75);
                    }
                    else
                    {
                        using (var stream = new FileStream(contentFilePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        
                    }
                    contentFileNames.Add(contentFileName);
                    _logger.LogInformation($"CourseStepController - Uploaded file '{file.FileName}' for Step {StepNo} of Course {CourseID}.");
                }

                var contentFilesString = string.Join(",", contentFileNames);

                // Save the description file
                var descriptionFileName = $"{Guid.NewGuid()}{Path.GetExtension(Description.FileName)}";
                var descriptionFilePath = Path.Combine(stepFolderPath, descriptionFileName);

                using (var stream = new FileStream(descriptionFilePath, FileMode.Create))
                {
                    Description.CopyTo(stream);
                }

                _logger.LogInformation($"CourseStepController - Saved description to '{descriptionFileName}' for Step {StepNo} of Course {CourseID}.");

                var existingCourseStep = _context.CourseStep.FirstOrDefault(cs => cs.CourseID == CourseID && cs.StepNo == StepNo);

                if (existingCourseStep != null)
                {
                    existingCourseStep.StepTitle = StepTitle;
                    existingCourseStep.StepContent = contentFilesString;
                    existingCourseStep.ContentType = ContentType;
                    existingCourseStep.Description = descriptionFileName;
                    _context.SaveChanges();
                    _logger.LogInformation($"CourseStepController - Updated existing course step for Step {StepNo} of Course {CourseID}.");
                }
                else
                {
                    var courseStep = new CourseStepModel
                    {
                        CourseID = CourseID,
                        StepNo = StepNo,
                        StepTitle = StepTitle,
                        StepContent = contentFilesString,
                        ContentType = ContentType,
                        Description = descriptionFileName
                    };

                    _context.CourseStep.Add(courseStep);
                    _context.SaveChanges();
                    _logger.LogInformation($"CourseStepController - Created new course step for Step {StepNo} of Course {CourseID}.");
                }

                return Ok("Files uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while uploading files: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




        // PUT: api/CourseStep/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] CourseStepModel updatedCourseStep)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("CourseStepController - Invalid model state detected while trying to update course step.");
                    return BadRequest(ModelState);
                }

                if (id != updatedCourseStep.ID)
                {
                    _logger.LogWarning("CourseStepController - ID mismatch detected while trying to update course step.");
                    return BadRequest();
                }

                _context.Entry(updatedCourseStep).State = EntityState.Modified;
                _context.SaveChanges();

                _logger.LogInformation($"CourseStepController - Course step with ID {id} updated successfully.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while updating course step with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // DELETE: api/CourseStep/RemoveFile
        [HttpDelete("removefile")]
        public IActionResult DeleteFileAndData(int CourseID, int StepID, string ContentType, string FileName)
        {
            try
            {
                var courseStep = _context.CourseStep
                    .Where(cs => cs.CourseID == CourseID && cs.StepNo == StepID)
                    .FirstOrDefault();

                if (courseStep == null)
                {
                    _logger.LogWarning($"CourseStepController - Course step with CourseID {CourseID} and StepID {StepID} not found.");
                    return NotFound();
                }

                var basePath = Path.Combine(_config["AssetFolder:AssetFolderPath"]);
                var stepFolderPath = Path.Combine(basePath, $"Course_{CourseID}", $"Step_{courseStep.StepNo}");
                var filePath = Path.Combine(stepFolderPath, FileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"CourseStepController - Deleted file '{FileName}' for CourseID {CourseID} and StepID {StepID}.");
                }
                else
                {
                    _logger.LogWarning($"CourseStepController - File '{FileName}' not found for CourseID {CourseID} and StepID {StepID}.");
                }

                if (ContentType == "Image")
                {
                    var imageFileNames = courseStep.StepContent.Split(',');
                    var updatedImageFileNames = imageFileNames.Where(name => name != FileName).ToList();

                    courseStep.StepContent = string.Join(",", updatedImageFileNames);
                    if (updatedImageFileNames.Count == 0)
                    {
                        courseStep.StepContent = "No Data";
                        courseStep.ContentType = "Text";
                    }

                    _context.SaveChanges();
                }
                else if (ContentType == "Video")
                {
                    courseStep.StepContent = "No Data";
                    courseStep.ContentType = "Text";
                    _context.SaveChanges();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while deleting file and data: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        // DELETE: api/CourseStep/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var courseStep = _context.CourseStep.Find(id);

                if (courseStep == null)
                {
                    _logger.LogWarning($"CourseStepController - Course step with ID {id} not found.");
                    return NotFound();
                }

                _context.CourseStep.Remove(courseStep);
                _context.SaveChanges();

                _logger.LogInformation($"CourseStepController - Course step with ID {id} deleted successfully.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while deleting course step with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // DELETE: api/CourseStep/5/StepNo
        [HttpDelete("deletestepno")]
        public IActionResult DeleteStepNo(int CourseID, int StepNo)
        {
            try
            {
                var courseStep = _context.CourseStep
                    .Where(cs => cs.CourseID == CourseID && cs.StepNo == StepNo)
                    .FirstOrDefault();

                if (courseStep == null)
                {
                    _logger.LogWarning($"CourseStepController - Course step with CourseID {CourseID} and StepNo {StepNo} not found.");
                    return NotFound();
                }

                var basePath = Path.Combine(_config["AssetFolder:AssetFolderPath"]);
                var stepFolderPath = Path.Combine(basePath, $"Course_{CourseID}", $"Step_{StepNo}");

                if (Directory.Exists(stepFolderPath))
                {
                    Directory.Delete(stepFolderPath, true);
                    _logger.LogInformation($"CourseStepController - Deleted folder for CourseID {CourseID}, StepNo {StepNo}.");
                }

                _context.CourseStep.Remove(courseStep);
                _context.SaveChanges();

                _logger.LogInformation($"CourseStepController - Deleted course step with CourseID {CourseID}, StepNo {StepNo}.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while deleting course step with CourseID {CourseID}, StepNo {StepNo}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        //GET: api/CourseStep/Course/5
        [HttpGet("Course/{id}")]
        public ActionResult<IEnumerable<CourseStepModel>> GetCourseSteps(int id)
        {
            try
            {
                var courseSteps = _context.CourseStep
                    .Where(cs => cs.CourseID == id)
                    .ToList();

                if (courseSteps == null || courseSteps.Count == 0)
                {
                    _logger.LogWarning($"CourseStepController - No course steps found for CourseID {id}.");
                    return NotFound();
                }

                _logger.LogInformation($"CourseStepController - Retrieved {courseSteps.Count} course steps for CourseID {id}.");
                return courseSteps;
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while retrieving course steps for CourseID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        [HttpGet("filecontent")]
        public IActionResult GetFileContent(int CourseID, int StepNo, string ContentType, string FileName)
        {

            //can also use the below commented code for getfilecontent method (the code is at end of the page)
            try
            {
                string folderPath;
                string contentType;

                switch (ContentType)
                {
                    case "Image":
                        folderPath = "Images";
                        contentType = GetContentType(FileName);
                        break;

                    case "Video":
                        folderPath = "Video";
                        contentType = GetContentType(FileName);
                        break;

                    case "HTML":
                        folderPath = "";
                        contentType = GetContentType(FileName);
                        break;

                    default:
                        _logger.LogWarning($"CourseStepController - Unknown file type '{ContentType}' requested.");
                        return BadRequest("Unknown File Type");
                }

                var basePath = Path.Combine(_config["AssetFolder:AssetFolderPath"]);
                var filesFolderPath = Path.Combine(basePath, $"Course_{CourseID}");
                var stepFolderPath = Path.Combine(filesFolderPath, $"Step_{StepNo}");
                var filePath = Path.Combine(stepFolderPath, FileName);

                if (System.IO.File.Exists(filePath))
                {
                    if (ContentType == "Image" || ContentType == "HTML")
                    {
                        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                        _logger.LogInformation($"CourseStepController - Retrieved file '{FileName}' for CourseID {CourseID}, StepNo {StepNo}.");
                        return File(fileStream, contentType);
                    }

                    if (ContentType == "Video")
                    {
                        var fileLength = new FileInfo(filePath).Length; // Check if the Range header is present
                        if (Request.Headers.ContainsKey("Range"))
                        {
                            var rangeHeader = Request.Headers["Range"].ToString();
                            var range = rangeHeader.Replace("bytes=", "").Split('-');
                            long start = string.IsNullOrEmpty(range[0]) ? 0 : Convert.ToInt64(range[0]);
                            long end = range.Length > 1 && !string.IsNullOrEmpty(range[1]) ? Convert.ToInt64(range[1]) : fileLength - 1;
                            if (start > end || end >= fileLength)
                            {
                                return BadRequest("Invalid Range header.");
                            }
                            var contentLength = end - start + 1;
                            var contentRange = $"bytes {start}-{end}/{fileLength}";
                            Response.Headers["Accept-Ranges"] = "bytes";
                            Response.Headers["Content-Range"] = contentRange;
                            Response.Headers["Content-Length"] = contentLength.ToString();
                            //Response.ContentType = "video/mp4";
                            Response.ContentType = contentType;
                            Response.StatusCode = (int)HttpStatusCode.PartialContent;
                            //fileStream.Seek(start, SeekOrigin.Begin); 
                            //return new FileStreamResult(fileStream, "video/mp4") 
                            //{ // FileDownloadName = fileName //}; 
                            var buffer = new byte[contentLength];
                            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                fileStream.Seek(start, SeekOrigin.Begin);
                                fileStream.Read(buffer, 0, buffer.Length);
                            }
                            return File(buffer, "video/mp4", enableRangeProcessing: true);
                        }
                        else
                        {
                            // Return the entire file if no Range header is present
                            return PhysicalFile(filePath, "video/mp4");
                        }
                    }

                }
                else
                {
                    _logger.LogWarning($"CourseStepController - File '{FileName}' not found for CourseID {CourseID}, StepNo {StepNo}.");
                    return Ok(new { Content = "File Not Found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseStepController - Error occurred while retrieving file for CourseID {CourseID}, StepNo {StepNo}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
            // Add a final return statement to handle any unexpected paths.
            return StatusCode(StatusCodes.Status500InternalServerError, "Unexpected error occurred.");
        }
        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".html" => "text/html",
                ".htm" => "text/html",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream",
            };
        }


    }
}

//[HttpGet("filecontent")]
//public IActionResult GetFileContent(int CourseID, int StepNo, string ContentType, string FileName)
//{
//    try
//    {
//        string contentType;

//        switch (ContentType)
//        {
//            case "Image":
//                contentType = GetContentType(FileName);
//                break;

//            case "Video":
//                contentType = "video/mp4";
//                break;

//            case "HTML":
//                contentType = GetContentType(FileName);
//                break;

//            default:
//                _logger.LogWarning($"CourseStepController - Unknown file type '{ContentType}' requested.");
//                return BadRequest("Unknown File Type");
//        }

//        var basePath = Path.Combine(_config["AssetFolder:AssetFolderPath"]);
//        var filesFolderPath = Path.Combine(basePath, $"Course_{CourseID}");
//        var stepFolderPath = Path.Combine(filesFolderPath, $"Step_{StepNo}");
//        var filePath = Path.Combine(stepFolderPath, FileName);

//        if (!System.IO.File.Exists(filePath))
//        {
//            _logger.LogWarning($"CourseStepController - File '{FileName}' not found for CourseID {CourseID}, StepNo {StepNo}.");
//            return NotFound(new { Content = "File Not Found" });
//        }

//        if (ContentType == "Image" || ContentType == "HTML")
//        {
//            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
//            _logger.LogInformation($"CourseStepController - Retrieved file '{FileName}' for CourseID {CourseID}, StepNo {StepNo}.");
//            return File(fileStream, contentType);
//        }
//        else if (ContentType == "Video")
//        {
//            var fileInfo = new FileInfo(filePath);
//            var fileLength = fileInfo.Length;

//            if (Request.Headers.ContainsKey("Range"))
//            {
//                var rangeHeader = Request.Headers["Range"].ToString();
//                var range = rangeHeader.Replace("bytes=", "").Split('-');
//                long start = string.IsNullOrEmpty(range[0]) ? 0 : Convert.ToInt64(range[0]);
//                long end = range.Length > 1 && !string.IsNullOrEmpty(range[1]) ? Convert.ToInt64(range[1]) : fileLength - 1;

//                if (start > end || end >= fileLength)
//                {
//                    return BadRequest("Invalid Range header.");
//                }

//                var contentLength = end - start + 1;
//                var contentRange = $"bytes {start}-{end}/{fileLength}";

//                Response.Headers["Accept-Ranges"] = "bytes";
//                Response.Headers["Content-Range"] = contentRange;
//                Response.Headers["Content-Length"] = contentLength.ToString();
//                Response.ContentType = contentType;
//                Response.StatusCode = (int)HttpStatusCode.PartialContent;

//                return new FileStreamResult(ReadStream(filePath, start, contentLength), contentType);
//            }
//            else
//            {
//                return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        _logger.LogError($"CourseStepController - Error occurred while retrieving file for CourseID {CourseID}, StepNo {StepNo}: {ex.Message}");
//        return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
//    }

//    return StatusCode(StatusCodes.Status500InternalServerError, "Unexpected error occurred.");
//}

//private string GetContentType(string fileName)
//{
//    var ext = Path.GetExtension(fileName).ToLowerInvariant();
//    return ext switch
//    {
//        ".jpg" => "image/jpeg",
//        ".jpeg" => "image/jpeg",
//        ".png" => "image/png",
//        ".gif" => "image/gif",
//        ".html" => "text/html",
//        ".htm" => "text/html",
//        ".mp4" => "video/mp4",
//        _ => "application/octet-stream",
//    };
//}

//private Stream ReadStream(string path, long offset, long length)
//{
//    var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
//    stream.Seek(offset, SeekOrigin.Begin);
//    return new SubStream(stream, length);
//}

//private class SubStream : Stream
//{
//    private readonly Stream _stream;
//    private long _remaining;

//    public SubStream(Stream stream, long length)
//    {
//        _stream = stream;
//        _remaining = length;
//    }

//    public override bool CanRead => _stream.CanRead;
//    public override bool CanSeek => _stream.CanSeek;
//    public override bool CanWrite => _stream.CanWrite;
//    public override long Length => _remaining;
//    public override long Position { get => _stream.Position; set => _stream.Position = value; }

//    public override void Flush() => _stream.Flush();

//    public override int Read(byte[] buffer, int offset, int count)
//    {
//        if (_remaining <= 0)
//            return 0;

//        var toRead = (int)Math.Min(count, _remaining);
//        var read = _stream.Read(buffer, offset, toRead);
//        _remaining -= read;
//        return read;
//    }

//    public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

//    public override void SetLength(long value) => _stream.SetLength(value);

//    public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
//}


