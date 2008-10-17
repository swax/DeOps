using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Services;


namespace RiseOp.Services.Update
{
    internal class UpdateService : OpService
    {
        public string Name { get { return "Update"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Update; } }

        OpCore Core;


        internal UpdateService(OpCore core)
        {
            Core = core;

            // look for update.dat file

            // read in hash/size/version info

            // load update header file, check update.dat exists with the right size

            // update header file - signed size, hash, key
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            return;
        }

        public void SimTest()
        {
            return;
        }

        public void SimCleanup()
        {
            return;
        }

        public void Dispose()
        {
            return;
        }

    }
}
