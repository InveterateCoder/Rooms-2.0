using System.Collections.Generic;

namespace Rooms.Models
{
    public class SearchReturn
    {
        public SearchReturn(int page, int total, IEnumerable<SearchRoom> list)
        {
            Page = page; Total = total;
            List = list;
        }
        public int Page {get;set;}
        public int Total {get;set;}
        public IEnumerable<SearchRoom> List {get;set;}
    }
    public class SearchRoom
    {
        public SearchRoom(string name, string slug,
            string description, string flag, bool locked, byte online)
        {
            Name = name; Slug = slug;
            Description = description;
            Flag = flag; Locked = locked;
            Online = online;
        }
        public string Name {get;set;}
        public string Slug {get;set;}
        public string Description {get;set;}
        public string Flag {get;set;}
        public bool Locked {get;set;}
        public byte Online {get;set;}
    }
}