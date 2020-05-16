using System;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rooms.Infrastructure;
using Rooms.Models;
using Microsoft.AspNetCore.SignalR;
using Rooms.Hubs;

namespace Rooms.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/[controller]")]
    [Produces("application/json")]
    public class RoomController : ControllerBase
    {
        private readonly Helper Helper;
        private readonly RoomsDBContext _context;
        private readonly State _state;
        private readonly IHubContext<RoomsHub> _hub;
        public RoomController(Helper helper, RoomsDBContext context, State state, IHubContext<RoomsHub> hub)
        {
            Helper = helper;
            _context = context;
            _state = state;
            _hub = hub;
        }
        [HttpPost("change")]
        public async Task<IActionResult> Change([FromBody] RoomForm form)
        {
            try
            {
                Identity id = JsonSerializer.Deserialize<Identity>(User.Identity.Name);
                if (id.Guest != null) return Forbid();
                if (!Helper.isRighGrouptName(form.Name)) return BadRequest(Errors.BadName);
                if (!StaticData.CountryCodes.Contains(form.Country)) return BadRequest(Errors.WrongCountry);
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.UserId == id.UserId);
                var slug = Helper.Slugify(form.Name);
                string[] connectionIds = null;
                if (room == null)
                {
                    if (await _context.Rooms.AnyAsync(r => r.Slug == slug))
                        return BadRequest(Errors.RoomNameExist);
                    await _context.Rooms.AddAsync(new Room
                    {
                        Name = form.Name,
                        Slug = slug,
                        Description = form.Description,
                        Country = form.Country,
                        Password = form.Password,
                        Limit = form.Limit,
                        UserId = id.UserId
                    });
                }
                else
                {
                    if (room.Slug != slug && await _context.Rooms.AnyAsync(r => r.Slug == slug))
                        return BadRequest(Errors.RoomNameExist);
                    if (room.Name != form.Name || room.Country != form.Country)
                        connectionIds = _state.Connections(room.RoomId);
                    room.Name = form.Name;
                    room.Slug = slug;
                    room.Description = form.Description;
                    room.Country = form.Country;
                    room.Password = form.Password;
                    room.Limit = form.Limit;
                    _context.Rooms.Update(room);
                }
                await _context.SaveChangesAsync();
                if (connectionIds != null)
                    await _hub.Clients.Clients(connectionIds).SendAsync("roomChanged",
                        new { name = form.Name, flag = form.Country });
                return Ok("ok");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
        [HttpGet("delete")]
        public async Task<IActionResult> Delete()
        {
            try
            {
                Identity id = JsonSerializer.Deserialize<Identity>(User.Identity.Name);
                if (id.Guest != null) return Forbid();
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.UserId == id.UserId);
                if (room == null) return BadRequest(Errors.NoRoom);
                var connections = _state.RemoveRoom(room.RoomId);
                if (connections != null)
                {
                    try
                    {
                        await _hub.Clients.Clients(connections.Select(c => c.ConnectionId).ToArray()).SendAsync("roomDeleted");
                    }
                    finally
                    {
                        foreach (var connection in connections)
                            connection.Abort();
                    }
                }
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
                return Ok("ok");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
    }
}