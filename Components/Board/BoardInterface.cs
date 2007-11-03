using System;
using System.Collections.Generic;
using System.Text;

using DeOps.Implementation;
using DeOps.Components.Link;


namespace DeOps.Components.Board
{


    internal class BoardInterface
    {
        internal OpCore Core;
        internal LinkControl Links;
        internal BoardControl Control;

        internal ThreadedDictionary<ulong, List<BoardView>> WindowMap = new ThreadedDictionary<ulong, List<BoardView>>();

        internal BoardInterface(BoardControl control)
        {
            
            Control = control;

            Core = control.Core;
            Links = Core.Links;
        }

        internal void LoadView(BoardView view, ulong id)
        {
            WindowMap.LockWriting(delegate()
            {
                if (!WindowMap.ContainsKey(id))
                    WindowMap[id] = new List<BoardView>();

                WindowMap[id].Add(view);
            });
        }

        internal void UnloadView(BoardView view, ulong id)
        {
            WindowMap.LockWriting(delegate()
           {
               if (!WindowMap.ContainsKey(id))
                   return;

               WindowMap[id].Remove(view);

               if (WindowMap[id].Count == 0)
                   WindowMap.Remove(id);
           });
        }

        internal List<ulong> GetBoardRegion(ulong id, uint project, ScopeType scope)
        {
            List<ulong> targets = new List<ulong>();

            targets.Add(id); // need to include self in high and low scopes, for re-searching, onlinkupdate purposes

            OpLink link = Links.GetLink(id);

            if (link == null)
                return targets;


            // get parent and children of parent
            if (scope != ScopeType.Low)
            {
                OpLink parent = link.GetHigher(project, true);

                if (parent != null)
                {
                    targets.Add(parent.DhtID);

                    targets.AddRange(Links.GetDownlinkIDs(parent.DhtID, project, 1));

                    targets.Remove(id); // remove self
                }
            }

            // get children of self
            if (scope != ScopeType.High)
                targets.AddRange(Links.GetDownlinkIDs(id, project, 1));


            return targets;
        }
    }
}
