using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Components.Plan
{
    enum EditItemMode { New, Edit, View };

    internal partial class EditPlanItem : Form
    {
        EditItemMode Mode;
        PlanGoal Goal;
        internal PlanItem Editing;


        internal EditPlanItem(EditItemMode mode, PlanGoal goal, PlanItem editing)
        {
            InitializeComponent();

            Mode = mode;
            Goal = goal;
            Editing = editing;

            TitleBox.Text = editing.Title;
            StartTime.Value = editing.Start.ToLocalTime();
            EndTime.Value = editing.End.ToLocalTime();
            CompletedHours.Text = editing.HoursCompleted.ToString();
            TotalHours.Text = editing.HoursTotal.ToString();
            DescriptionInput.InputBox.Rtf = editing.Description;

            if (Mode == EditItemMode.New)
                Text = "New Plan Item";

            if (Mode == EditItemMode.Edit)
                Text = "Edit Plan Item";

            if (Mode == EditItemMode.View)
            {
                Text = editing.Title;

                TitleBox.ReadOnly = true;
                StartTime.Enabled = false;
                EndTime.Enabled = false;
                CompletedHours.ReadOnly = true;
                TotalHours.ReadOnly = true;
                DescriptionInput.InputBox.ReadOnly = true;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (Mode == EditItemMode.View)
            {
                Close();
                return;
            }

            try
            {
                // check title
                if (TitleBox.Text == "")
                    throw new Exception("Title cannot be Empty");

                // check deadline
                if (StartTime.Value > EndTime.Value)
                    throw new Exception("Start Time cannot be set after End Time");

                if (EndTime.Value.ToUniversalTime() > Goal.End)
                    throw new Exception("End Time cannot be set after Job Deadline");


                int total = int.Parse(TotalHours.Text);
                int completed = int.Parse(CompletedHours.Text);

                if (completed > total)
                    throw new Exception("Completed hours cannot be greater than total hours");


                Editing.Title = TitleBox.Text;
                Editing.Start = StartTime.Value.ToUniversalTime();
                Editing.End = EndTime.Value.ToUniversalTime();
                Editing.HoursCompleted = completed;
                Editing.HoursTotal = total;
                Editing.Description = DescriptionInput.InputBox.Rtf;

                // signal commit
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