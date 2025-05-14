using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.BaseIDatatem.input
{
    public class DataItemInput
    {

        [Required]
        public string ItemCode { get; set; }
    }
}
