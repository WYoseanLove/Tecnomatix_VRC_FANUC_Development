using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecnomatix.Engineering;

namespace VRC_FANUC
{
    public class TxVRCRoboGuideComm:TxButtonCommand
    {

        public override string Category
        {

            get
            {
                return StringTable.Catergory;
            }
        }

        public override string Name
        {

            get
            {
                return StringTable.Name;
            }
        }

        public override void Execute(object cmdParams)
        {
            TxVRCRoboGuideComForm form = new TxVRCRoboGuideComForm();

            form.Show();

        }

        public override string LargeBitmap
        {

            get
            {
                return "vrc_png.png";
            }
        }




    }

}
