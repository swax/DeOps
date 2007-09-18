using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;


namespace DeOps.Components.Plan
{
    internal enum BlockViewMode { New, Edit, Show };

    internal partial class EditBlock : Form
    {
        BlockViewMode Mode;

        OpCore Core;
        PlanControl Plans;
        ScheduleView View;

        PlanBlock Block;


        internal EditBlock(BlockViewMode mode, ScheduleView view, PlanBlock block)
        {
            InitializeComponent();

            Mode = mode;
            View = view;
            Core = view.Core;
            Plans = view.Plans;
            Block = block;

            StartTime.Value = new DateTime(Core.TimeNow.Year, Core.TimeNow.Month, Core.TimeNow.Day);
            EndTime.Value   = StartTime.Value.AddDays(1);
            
            if (block == null)
                return;

            TitleBox.Text = block.Title;
            StartTime.Value = block.StartTime.ToLocalTime();
            EndTime.Value = block.EndTime.ToLocalTime();
            DescriptionInput.InputBox.Rtf = block.Description;
            PersonalCheck.Checked = block.Personal;

            if (mode != BlockViewMode.Show)
                return;

            TitleBox.ReadOnly = true;
            StartTime.Enabled = false;
            EndTime.Enabled = false;
            DescriptionInput.ReadOnly = true;
            PersonalCheck.Enabled = false;
        }

        internal void BlockView_Load(object sender, EventArgs e)
        {
            if (Mode == BlockViewMode.New)
            {
                Text = "New Schedule Block";
                OkButton.Text = "Create";
            }

            if(Mode == BlockViewMode.Edit)
            {
                Text = "Edit Schedule Block";
                OkButton.Text = "Edit";
            }

            if (Mode == BlockViewMode.Show)
            {
                Text = Core.Links.GetName(View.DhtID) + "'s Block";
                OkButton.Hide();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (Mode == BlockViewMode.Show)
            {
                Close();
                return;
            }

            if (EndTime.Value < StartTime.Value)
            {
                MessageBox.Show("Start time must be earlier than End time");
                return;
            }

            if (Block != null && Plans.LocalPlan.Blocks.ContainsKey(Block.ProjectID))
                Plans.LocalPlan.Blocks[Block.ProjectID].Remove(Block);


            PlanBlock block = (Block != null) ? Block : new PlanBlock();

            block.ProjectID     = View.ProjectID;
            block.Title         = TitleBox.Text;
            block.StartTime     = StartTime.Value.ToUniversalTime();
            block.EndTime       = EndTime.Value.ToUniversalTime();
            block.Description   = DescriptionInput.InputBox.Rtf;
            block.Personal      = PersonalCheck.Checked;

            if (Mode == BlockViewMode.New)
                block.Unique = Core.RndGen.Next();

            // add to local plan
            Plans.LocalPlan.AddBlock(block);
            
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}