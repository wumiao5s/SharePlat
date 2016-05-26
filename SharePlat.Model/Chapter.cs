using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePlat.Model
{
    public class Chapter
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime PostDate { get; set; }
        public int CaiCount { get; set; }
        public int ZanCount { get; set; }
        public int UserId { get; set; }

        //add 
        [IngorePropertyAttribute]
        public string UserName { get; set; }
        [IngorePropertyAttribute]
        public string BlogUrl { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IngorePropertyAttribute : Attribute
    {

    }
}
