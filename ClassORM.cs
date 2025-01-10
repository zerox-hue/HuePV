using ModKit.ORM;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuePV
{
    public class OrmClassPV : ModEntity<OrmClassPV>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }

        public string Plaque { get; set; }
        public bool Payer { get; set; }     
    }
}
