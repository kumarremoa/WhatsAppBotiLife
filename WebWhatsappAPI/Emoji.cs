using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebWhatsappAPI
{
  public sealed  class Emoji
    {

        public static readonly Emoji Ketawa = new Emoji(":-d");
        public static readonly Emoji Senyum = new Emoji(":-)");
        public static readonly Emoji Jempol = new Emoji("(y)");
        public static readonly Emoji Khawatir = new Emoji(":-(");
        public static readonly Emoji Cinta = new Emoji("<3");

        public readonly string NamaEmoji; 
        private Emoji(string namaemoji)
        {
            NamaEmoji = namaemoji;
        }

    }
}
