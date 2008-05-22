using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Interface;
using RiseOp.Implementation;


namespace RiseOp.Services.Plan
{
    internal enum BlockViewMode { New, Edit, Show };

    internal partial class EditBlock : Form
    {
        BlockViewMode Mode;

        OpCore Core;
        PlanService Plans;
        ScheduleView View;

        PlanBlock Block;

        short CurrentScope = -1; // everyone default


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
            DescriptionInput.InputBox.Text = block.Description;
            SetScopeLink(block.Scope);

            if (mode != BlockViewMode.Show)
                return;

            TitleBox.ReadOnly = true;
            StartTime.Enabled = false;
            EndTime.Enabled = false;
            DescriptionInput.ReadOnly = true;
            ScopeLink.Enabled = false;
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
                Text = Core.Links.GetName(View.UserID) + "'s Block";
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
            block.Description   = DescriptionInput.InputBox.Text;
            block.Scope         = CurrentScope;

            if (Mode == BlockViewMode.New)
                block.Unique = Core.RndGen.Next();

            // add to local plan
            Plans.LocalPlan.AddBlock(block);
            
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ScopeLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GetTextDialog getText = new GetTextDialog("Sub-Levels", "How many levels down is this item visible? 0 for Personal, -1 for Everyone", CurrentScope.ToString());

            if (getText.ShowDialog() == DialogResult.OK)
            {
                short levels;
                short.TryParse(getText.ResultBox.Text, out levels);

                SetScopeLink(levels);
            }
        }

        private void SetScopeLink(short scope)
        {
            if (scope == -1)
                ScopeLink.Text = "Everyone";

            else if (scope == 0)
                ScopeLink.Text = "Personal";

            else
                ScopeLink.Text = scope.ToString() + " Sub-Levels";

            CurrentScope = scope;
        }
    }
}