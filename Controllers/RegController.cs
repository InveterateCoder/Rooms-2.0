using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rooms.Models;
using Microsoft.AspNetCore.Http;
using Rooms.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Rooms.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    [Produces("application/json")]
    public class RegController : ControllerBase
    {
        private readonly Helper Helper;
        private readonly RoomsDBContext _context;
        public RegController(Helper helper, RoomsDBContext context)
        {
            Helper = helper;
            _context = context;
        }
        [HttpPost("reg")]
        public async Task<IActionResult> Register([FromBody]RegForm data)
        {
            try
            {
                if (!Helper.isRightName(data.Name)) return BadRequest(Errors.BadName);
                if (_context.Users.FirstOrDefault(u => u.Email == data.Email) != null)
                    return BadRequest(Errors.EmailTaken);
                var limit = DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0)).Ticks;
                var range = _context.RegQueue.Where(e => e.Date < limit);
                _context.RegQueue.RemoveRange(range);
                var rand = new Random();
                string key;
                do key = rand.Next(100000000, 999999999).ToString();
                while (_context.RegQueue.FirstOrDefault(e => e.Key == key) != null);
                await _context.RegQueue.AddAsync(new RegQueueEntity
                {
                    Key = key,
                    Date = DateTime.UtcNow.Ticks,
                    Name = data.Name,
                    Email = data.Email,
                    Password = data.Password
                });
                await _context.SaveChangesAsync();
                string content = $"Hello {data.Name}. Please follow the link to confirm your email addresss.\n{Helper.Settings.EmailConfirmAddr + key}";
                if (!await Helper.SendMail(data.Email, content))
                    throw new Exception("Failed to send a confirmation email.");
                return Ok("ok");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromBody]string key)
        {
            try
            {
                var entity = _context.RegQueue.FirstOrDefault(e => e.Key == key);
                if (entity == null) return BadRequest(Errors.ConfEmailNotFound);
                if (_context.Users.FirstOrDefault(u => u.Email == entity.Email) != null)
                    return BadRequest(Errors.EmailTaken);
                var user = await _context.Users.AddAsync(new Models.User
                {
                    Name = entity.Name,
                    Email = entity.Email,
                    Password = entity.Password,
                    Room = null
                });
                _context.RegQueue.Remove(entity);
                await _context.SaveChangesAsync();
                Identity id = new Identity
                {
                    UserId = user.Entity.UserId,
                    Name = user.Entity.Name
                };
                return Ok(new
                {
                    jwt = Helper.GetToken(JsonSerializer.Serialize(id)),
                    name = user.Entity.Name
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
        [HttpPost("sign/user")]
        public IActionResult SignIn([FromBody] SignInForm form)
        {
            try
            {
                var user = _context.Users.Include(u => u.Room).FirstOrDefault(u => u.Email == form.Email);
                if (user == null || user.Password != form.Password)
                    return BadRequest(Errors.EmailOrPassInc);
                Identity id = new Identity
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    Guest = null
                };
                return Ok(new
                {
                    jwt = Helper.GetToken(JsonSerializer.Serialize(id)),
                    userId = id.UserId,
                    userGuid = id.Guest,
                    name = user.Name,
                    room = user.Room == null ? null : new
                    {
                        name = user.Room.Name,
                        description = user.Room.Description,
                        country = user.Room.Country,
                        password = user.Room.Password,
                        limit = user.Room.Limit
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
        [HttpGet("sign/guest")]
        public IActionResult SignInGuest([Required, StringLength(10, MinimumLength = 4)]string name)
        {
            if (!Helper.isRightName(name)) return BadRequest(Errors.BadName);
            Identity id = new Identity
            {
                UserId = 0,
                Name = name,
                Guest = Guid.NewGuid().ToString("n")
            };
            return Ok(new
            {
                jwt = Helper.GetToken(JsonSerializer.Serialize(id)),
                userId = id.UserId,
                userGuid = id.Guest
            });
        }
        [HttpPost("recover")]
        public async Task<IActionResult> Recover([Required, FromBody, StringLength(320, MinimumLength = 6)]string email)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == email);
                if (user == null)
                    return BadRequest(Errors.NotRegistered);
                string content = $"Hello {user.Name}. Your password is {user.Password}";
                if (!await Helper.SendMail(user.Email, content))
                    throw new Exception("Failed to send a confirmation email.");
                return Ok("ok");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
    }
}