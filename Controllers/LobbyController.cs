using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rooms.Infrastructure;
using Rooms.Models;

namespace Rooms.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/[controller]")]
    [Produces("application/json")]
    public class LobbyController : ControllerBase
    {
        private readonly Helper Helper;
        private readonly RoomsDBContext _context;
        private readonly State _state;
        public LobbyController(Helper helper, RoomsDBContext context, State state)
        {
            Helper = helper;
            _context = context;
            _state = state;
        }
        [HttpGet("search/{page:min(1)}/{perpage:min(10)}")]
        public async Task<IActionResult> Search(int page, int perpage,
            [FromQuery] string slug, [FromQuery] string c_codes)
        {
            try
            {
                return await Task<IActionResult>.Factory.StartNew(() =>
                {
                    string[] cs = c_codes == null ? null : c_codes.Split('_');
                    if (cs != null)
                        foreach (var country in cs)
                            if (!StaticData.CountryCodes.Contains(country))
                                return BadRequest(Errors.BadQuery);
                    if (slug != null)
                    {
                        if (!Helper.isRighGrouptName(slug, true))
                            return BadRequest(Errors.BadQuery);
                    }
                    IQueryable<Room> rooms;
                    if (string.IsNullOrEmpty(slug)) rooms = _context.Rooms.AsQueryable();
                    else rooms = _context.Rooms.Where(r => r.Slug.StartsWith(slug, StringComparison.OrdinalIgnoreCase));
                    if (cs != null && cs.Length > 0)
                        rooms = rooms.Where(r => cs.Contains(r.Country));
                    int count = rooms.Count();
                    if (count <= 0) return Ok(new SearchReturn(1, 0, null));
                    int total_pages = count / perpage + (count % perpage > 0 ? 1 : 0);
                    if (page > total_pages) page = total_pages;
                    int activeCount = 0;
                    IQueryable<Room> activeRooms = null;
                    if (_state.RoomsCount > 0)
                    {
                        var activeIds = _state.RoomsKeys;
                        activeRooms = rooms.Where(r => activeIds.Contains(r.RoomId));
                        activeCount = activeRooms.Count();
                        if (activeCount > 0)
                        {
                            activeRooms = activeRooms.OrderBy(r => r.Slug);
                            rooms = rooms.Where(r => !activeIds.Contains(r.RoomId));
                        }
                    }
                    rooms = rooms.OrderBy(r => r.Slug);
                    int skip = (page - 1) * perpage;
                    int skip_remain = activeCount - skip;
                    IEnumerable<SearchRoom> list = null;
                    if (skip_remain > 0)
                    {
                        list = activeRooms.Skip(skip).Take(perpage).Select(r => new SearchRoom(r.Name, r.Slug,
                            r.Description, r.Country, r.Password != null ? true : false,
                            _state.GetRoom(r.RoomId).Online)).ToArray();
                        if (list.Count() < perpage)
                        {
                            int take = perpage - list.Count();
                            var remnant = rooms.Take(take).Select(r => new SearchRoom(r.Name, r.Slug,
                                r.Description, r.Country, r.Password != null ? true : false, 0)).ToArray();
                            list = list.Concat(remnant);
                        }
                    }
                    else
                    {
                        skip_remain = Math.Abs(skip_remain);
                        list = rooms.Skip(skip_remain).Take(perpage).Select(r => new SearchRoom(r.Name, r.Slug,
                            r.Description, r.Country, r.Password != null ? true : false, 0)).ToArray();
                    }
                    return Ok(new SearchReturn(page, total_pages, list));
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
    }
}