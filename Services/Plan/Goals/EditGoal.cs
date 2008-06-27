using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Services.Trust;

namespace RiseOp.Services.Plan
{
    enum EditGoalMode { New, Delgate, Edit, View };

    internal partial class EditGoal : RiseOp.Interface.CustomIconForm
    {
        EditGoalMode Mode;
        OpCore Core;
        GoalsView View;

        ulong PersonID;

        PlanGoal Editing;


        internal EditGoal(EditGoalMode mode, GoalsView view, PlanGoal editing)
        {
            InitializeComponent();

            Mode = mode;
            View = view;
            Core = View.Core;
            Editing = editing;

            if (Mode == EditGoalMode.New)
            {
                Text = "New Goal";
                PersonLabel.Visible = false;
                PickLink.Visible = false;
            }

            if (Mode == EditGoalMode.Delgate)
                Text = "Delegate Responsibility";

            if (Mode == EditGoalMode.View)
            {
                Text = editing.Title;

                TitleBox.ReadOnly = true;
                Deadline.Enabled = false;
                PickLink.Enabled = false;
                NotesInput.ReadOnly = true;
            }

            if (Mode == EditGoalMode.Edit)
                Text = "Edit Goal";

            TitleBox.Text = Editing.Title;
            Deadline.Value = Editing.End.ToLocalTime();
            SetPerson(Editing.Person);
            NotesInput.InputBox.Text = Editing.Description;


        }

        private void SetPerson(ulong id)
        {
            PersonID = id;

            if (id != 0)
                PickLink.Text = Core.Trust.GetName(PersonID);
        }

        private void PickLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AddLinks add = new AddLinks(Core.Trust, Editing.Project);

            // force root to self, only if self is not in a loop, in which case anyone in loop can be assigned sub-goals
            //if(!Core.Links.LocalLink.LoopRoot.ContainsKey(Editing.Project)), assignment loops, not obvious behavior
                add.PersonTree.ForceRootID = Core.UserID;
            
            add.PersonTree.HideUnlinked = true;
            add.ProjectCombo.Visible = false;

            if (add.ShowDialog(this) == DialogResult.OK)
                if (add.People.Count > 0)
                    SetPerson(add.People[0]);
                
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (Mode == EditGoalMode.View)
            {
                Close();
                return;
            }

            try
            {
                // check title
                if (TitleBox.Text == "")
                    throw new Exception("Title cannot be empty");


                // check people
                if (Mode != EditGoalMode.New && PersonID == 0)
                    throw new Exception("Person must be selected");


                if (Mode != EditGoalMode.New)
                    Editing.Person = PersonID;


                Editing.Title = TitleBox.Text;
                Editing.End = Deadline.Value.ToUniversalTime();
                Editing.Description = NotesInput.InputBox.Text;


                if (Mode == EditGoalMode.New || Mode == EditGoalMode.Delgate)
                    View.Plans.LocalPlan.AddGoal(Editing);


                DialogResult = DialogResult.OK;

                Close();
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }


    }
}