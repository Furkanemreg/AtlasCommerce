using AutoMapper;
using AtlasCommerce.Application.Extensions;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MessageController : BaseController<UserMessage>
    {
        private readonly ILogger<MessageController> _logger;
        private readonly IMapper _mapper;
        private readonly IBaseService<WebsiteSettings> _settingsService;

        public MessageController(IBaseService<UserMessage> baseService, IUnitOfWork unitOfWork, ILogger<MessageController> logger, IMapper mapper, IBaseService<WebsiteSettings> settingsService) : base(baseService, unitOfWork)
        {
            _logger = logger;
            _mapper = mapper;
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, int messageType = 0)
        {
            // MessageType = 0 => Tümü (Silinmemişler)
            // MessageType = 1 => Silinmiş Mesajlar | 2 => Okunmamış Mesajlar | 3 => Okunmuş Mesajlar
            Expression<Func<UserMessage, bool>> predicate = x => true;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.ToLower();
                predicate = predicate.And(x =>
                    (!string.IsNullOrEmpty(x.Name) && x.Name.ToLower().Contains(term)) ||
                    (!string.IsNullOrEmpty(x.Surname) && x.Surname.ToLower().Contains(term)) ||
                    (!string.IsNullOrEmpty(x.EmailAddress) && x.EmailAddress.ToLower().Contains(term)) ||
                    (!string.IsNullOrEmpty(x.PhoneNumber) && x.PhoneNumber.ToLower().Contains(term)) ||
                    (!string.IsNullOrEmpty(x.Topic) && x.Topic.ToLower().Contains(term))
                );
            }

            if (messageType.Equals(2) || messageType.Equals(3))
            {
                predicate = predicate.And(messageType switch
                {
                    2 => x => x.IsRead.Equals(false),
                    3 => x => x.IsRead.Equals(true)
                });
            }

            List<UserMessage>? entities = new List<UserMessage>();
            int totalCount = 0;

            if (messageType.Equals(1))
                (entities, totalCount) = await _baseService.GetPagedAsync(pageNumber, pageSize, predicate, true);
            else
                (entities, totalCount) = await _baseService.GetPagedAsync(pageNumber, pageSize, predicate, false);

            var vmList = _mapper.Map<List<MessageVM>>(entities);

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            var model = new FilteredResult<MessageVM>
            {
                Items = vmList,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchTerm = searchTerm,
                MessageType = messageType,
                Settings = settings,
            };

            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> MarkAllAsRead()
        {
            var unreadMessages = await _baseService.GetAllAsync(i => i.IsRead.Equals(false));
            if (unreadMessages == null || !unreadMessages.Any())
                return Json(new { success = false, message = "Hiç okunmuş mesaj bulunamadı." });

            foreach (var message in unreadMessages)
                message.IsRead = true;

            await _baseService.UpdateRangeAsync(unreadMessages);
            await _unitOfWork.Commit();

            return Json(new { success = true, message = string.Format("{0} {1}", unreadMessages.Count, "mesaj okundu olarak güncellendi.") });
        }

        [HttpGet]
        public async Task<JsonResult> MarkAllAsUnread()
        {
            var readMessages = await _baseService.GetAllAsync(i => i.IsRead.Equals(true));
            if (readMessages == null || !readMessages.Any())
                return Json(new { success = false, message = "Hiç okunmamış mesaj bulunamadı." });

            foreach (var message in readMessages)
                message.IsRead = false;

            await _baseService.UpdateRangeAsync(readMessages);
            await _unitOfWork.Commit();

            return Json(new { success = true, message = string.Format("{0} {1}", readMessages.Count, "mesaj okunmadı olarak güncellendi.") });
        }

        [HttpPost]
        public async Task<JsonResult> UnreadMessage(Guid id)
        {
            var message = await _baseService.GetByIdAsync(id);
            if (message == null)
                return Json(new { success = false, message = "Seçilen mesaj bulunamadı." });

            message.IsRead = false;
            await _baseService.UpdateAsync(message);
            await _unitOfWork.Commit();

            return Json(new { success = true, message = string.Format("{0} {1}", message.Topic, "konulu mesaj okunmadı olarak güncellendi.") });
        }

        [HttpPost]
        public async Task<JsonResult> ReadMessage(Guid id)
        {
            var message = await _baseService.GetByIdAsync(id);
            if (message == null)
                return Json(new { success = false, message = "Seçilen mesaj bulunamadı." });

            message.IsRead = true;
            await _baseService.UpdateAsync(message);
            await _unitOfWork.Commit();

            return Json(new { success = true, message = string.Format("{0} {1}", message.Topic, "konulu mesaj okundu olarak güncellendi.") });
        }

        [HttpPost]
        public async Task<JsonResult> DeleteMessage(Guid id)
        {
            var message = await _baseService.GetByIdAsync(id);
            if (message == null)
                return Json(new { success = false, message = "Seçilen mesaj bulunamadı." });

            await _baseService.DeleteAsync(message);
            await _unitOfWork.Commit();

            return Json(new { success = true, message = string.Format("{0} {1}", message.Topic, "konulu mesaj silindi.") });
        }

        [HttpGet]
        public async Task<IActionResult> View(Guid id)
        {
            if (id.Equals(Guid.Empty))
                throw new ArgumentNullException("id");

            UserMessage message = await _baseService.GetByIdAsync(id);
            if (message == null)
                throw new Exception("Seçilen mesaj bulunamadı.");

            message.IsRead = true;
            await _baseService.UpdateAsync(message);
            await _unitOfWork.Commit();

            MessageVM vm = _mapper.Map<MessageVM>(message);

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);
            vm.Settings = settings;

            return View(vm);
        }

        [HttpGet("Message/ViewMessage")]
        public async Task<IActionResult> ViewMessage(Guid id)
        {
            if (id == null || id == Guid.Empty)
                return BadRequest("Message ID could not be found.");

            var message = await _baseService.GetByIdAsync(id);
            if (message == null)
                return BadRequest("Message could not be found.");

            message.IsRead = true;


            await _baseService.UpdateAsync(message);
            await _unitOfWork.Commit();
            return Json(new
            {
                Name = message.Name,
                Surname = message.Surname,
                Emailaddress = message.EmailAddress,
                Messagedate = message.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                Topic = message.Topic,
                Message = message.Message
            });
        }
    }
}
