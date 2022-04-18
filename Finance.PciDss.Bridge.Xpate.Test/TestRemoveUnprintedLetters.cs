using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Finance.PciDss.Bridge.Xpate.Test
{
    class TestRemoveUnprintedLetters
    {
        [TestCase("!@#$%^&*()_+=-~`Ss/.,<>?;':\"[{]}'", "Ss")]
        [TestCase("3312XV ", "3312XV")]
        [TestCase("33-12XV ", "3312XV")]
        public void Send_Xpate_SaleRequest_And_Check_Status(string rawZipCode, string clearZipCode)
        {
            var modifiedZipCode = new String(rawZipCode.Where(Char.IsLetterOrDigit).ToArray());
            Assert.AreEqual(modifiedZipCode, clearZipCode);
        }
    }
}
