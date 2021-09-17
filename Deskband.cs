using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using CSDeskBand;
using CSDeskBand.ContextMenu;

namespace GlobalMediaControl
{
    [ComVisible(true)]
    [Guid("515F2CAA-73E3-41C3-B224-178DCC7CCA4A")]
    [CSDeskBandRegistration(Name = "GlobalMediaControl")]
    public class Deskband : CSDeskBandWpf
    {
        public Deskband()
        {
            Options.ContextMenuItems = ContextMenuItems;
        }

        protected override UIElement UIElement => new MediaControlBar();

        private List<DeskBandMenuItem> ContextMenuItems
        {
            get
            {
                var action = new DeskBandMenuAction("Night Mode");
                action.Clicked += Action_Clicked;
                return new List<DeskBandMenuItem>() { action, };
            }
        }

        private void Action_Clicked(object sender, EventArgs e)
        {
            Properties.Settings.Default.NightMode = !Properties.Settings.Default.NightMode;
        }
    }
}
