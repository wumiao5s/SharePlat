using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePlat.Model
{
    public class ChapterComment
    {
        public int Id{get;set;}
        public string Comment { get; set; }
        public DateTime PostTime { get; set; }
        public int ChapterId { get; set; }
        public int UserId { get; set; }

        //add
        public string UserName { get; set; }
    }
}
