using System;
using System.Linq;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.Generic;

namespace CKAN
{
    public partial class Main
    {
        #region Filter dropdown

        private void FilterToolButton_DrodDown_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Remove any existing custom labels from the list
            for (int i = FilterToolButton.DropDownItems.Count - 1; i >= 0; --i)
            {
                if (FilterToolButton.DropDownItems[i] == tagFilterToolStripSeparator)
                {
                    // Stop when we get to the first separator
                    break;
                }
                FilterToolButton.DropDownItems.RemoveAt(i);
            }
            // Tags
            foreach (var kvp in mainModList.ModuleTags.Tags.OrderBy(kvp => kvp.Key))
            {
                FilterToolButton.DropDownItems.Add(new ToolStripMenuItem(
                    $"{kvp.Key} ({kvp.Value.ModuleIdentifiers.Count})",
                    null, tagFilterButton_Click
                )
                {
                    Tag = kvp.Value
                });
            }
            FilterToolButton.DropDownItems.Add(new ToolStripMenuItem(
                string.Format(Properties.Resources.MainLabelsUntagged, mainModList.ModuleTags.Untagged.Count),
                null, tagFilterButton_Click
            )
            {
                Tag = null
            });
            FilterToolButton.DropDownItems.Add(customFilterToolStripSeparator);
            // Labels
            foreach (ModuleLabel mlbl in mainModList.ModuleLabels.Labels)
            {
                FilterToolButton.DropDownItems.Add(new ToolStripMenuItem(
                    $"{mlbl.Name} ({mlbl.ModuleIdentifiers.Count})",
                    null, customFilterButton_Click
                )
                {
                    Tag = mlbl
                });
            }
        }

        private void tagFilterButton_Click(object sender, EventArgs e)
        {
            var clicked = sender as ToolStripMenuItem;
            Filter(GUIModFilter.Tag, clicked.Tag as ModuleTag, null);
        }

        private void customFilterButton_Click(object sender, EventArgs e)
        {
            var clicked = sender as ToolStripMenuItem;
            Filter(GUIModFilter.CustomLabel, null, clicked.Tag as ModuleLabel);
        }

        #endregion

        #region Right click menu

        private void LabelsContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LabelsContextMenuStrip.Items.Clear();

            var module = GetSelectedModule();
            foreach (ModuleLabel mlbl in mainModList.ModuleLabels.Labels)
            {
                if (mlbl.InstanceName == null || mlbl.InstanceName == CurrentInstance.Name)
                {
                    LabelsContextMenuStrip.Items.Add(
                        new ToolStripMenuItem(mlbl.Name, null, labelMenuItem_Click)
                        {
                            Checked      = mlbl.ModuleIdentifiers.Contains(module.Identifier),
                            CheckOnClick = true,
                            Tag          = mlbl,
                        }
                    );
                }
            }
            LabelsContextMenuStrip.Items.Add(labelToolStripSeparator);
            LabelsContextMenuStrip.Items.Add(editLabelsToolStripMenuItem);
            e.Cancel = false;
        }

        private void labelMenuItem_Click(object sender, EventArgs e)
        {
            var item   = sender   as ToolStripMenuItem;
            var mlbl   = item.Tag as ModuleLabel;
            var module = GetSelectedModule();
            if (item.Checked)
            {
                mlbl.Add(module.Identifier);
            }
            else
            {
                mlbl.Remove(module.Identifier);
            }
            mainModList.ReapplyLabels(module, Conflicts?.ContainsKey(module) ?? false);
            mainModList.ModuleLabels.Save(ModuleLabelList.DefaultPath);
        }

        private void editLabelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditLabelsDialog eld = new EditLabelsDialog(currentUser, Manager, mainModList.ModuleLabels);
            eld.ShowDialog(this);
            eld.Dispose();
            mainModList.ModuleLabels.Save(ModuleLabelList.DefaultPath);
        }

        #endregion

        #region Notifications

        private void LabelsAfterUpdate(IEnumerable<GUIMod> mods)
        {
            Util.Invoke(Main.Instance, () =>
            {
                var notifLabs = mainModList.ModuleLabels.Labels.Where(l => l.NotifyOnChange);
                var toNotif = mods
                    .Where(m =>
                        notifLabs.Any(l =>
                            l.ModuleIdentifiers.Contains(m.Identifier)))
                    .Select(m => m.Name)
                    .ToArray();
                if (toNotif.Any())
                {
                    MessageBox.Show(
                        string.Format(
                            Properties.Resources.MainLabelsUpdateMessage,
                            string.Join("\r\n", toNotif)
                        ),
                        Properties.Resources.MainLabelsUpdateTitle,
                        MessageBoxButtons.OK
                    );
                }

                foreach (GUIMod mod in mods)
                {
                    foreach (ModuleLabel l in mainModList.ModuleLabels.Labels
                        .Where(l => l.RemoveOnChange
                            && l.ModuleIdentifiers.Contains(mod.Identifier)))
                    {
                        l.Remove(mod.Identifier);
                    }

                }
            });
        }

        private bool AnyLabelAlertsBeforeInstall(CkanModule mod)
        {
            return mainModList.ModuleLabels.Labels
                .Where(l => l.AlertOnInstall)
                .Any(l => l.ModuleIdentifiers.Contains(mod.identifier));
        }

        private void LabelsAfterInstall(CkanModule mod)
        {
            foreach (ModuleLabel l in mainModList.ModuleLabels.Labels
                .Where(l => l.RemoveOnInstall
                    && l.ModuleIdentifiers.Contains(mod.identifier)))
            {
                l.Remove(mod.identifier);
            }
        }

        #endregion
	}
}
