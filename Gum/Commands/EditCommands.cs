﻿using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolCommands;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ToolsUtilities;

namespace Gum.Commands
{
    public class EditCommands
    {
        #region State

        public void AddState()
        {
            if(SelectedState.Self.SelectedStateCategorySave == null && SelectedState.Self.SelectedElement == null )
            {
                MessageBox.Show("You must first select an element or a behavior category to add a state");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new state name:";

                if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string name = tiw.Result;

                    StateSave stateSave = ElementCommands.Self.AddState(
                        SelectedState.Self.SelectedElement, SelectedState.Self.SelectedStateCategorySave, name);

                    StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedStateContainer);

                    SelectedState.Self.SelectedStateSave = stateSave;

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentObject();
                }
            }
        }

        public void RemoveState(StateSave stateSave, IStateContainer stateContainer)
        {
            var behaviorNeedingState = GetBehaviorsNeedingState(stateSave);

            if (behaviorNeedingState.Any())
            {
                string message =
                    "This state cannot be removed because it is needed by the following behavior(s):";

                foreach (var behavior in behaviorNeedingState)
                {
                    message += "\n" + behavior.Name;
                }

                MessageBox.Show(message);
            }
            else if(stateSave.ParentContainer?.DefaultState == stateSave)
            {
                string message =
                    "This state cannot be removed because it is the default state.";
                MessageBox.Show(message);
            }
            else
            {
                var response = MessageBox.Show($"Are you sure you want to delete the state {stateSave.Name}?", "Delete state?", MessageBoxButtons.YesNo);

                if (response == DialogResult.Yes)
                {
                    ObjectRemover.Self.Remove(stateSave);
                }
            }
        }

        internal void RenameState(StateSave stateSave, IStateContainer stateContainer)
        {
            var behaviorNeedingState = GetBehaviorsNeedingState(stateSave);

            if (behaviorNeedingState.Any())
            {
                string message =
                    "This state cannot be removed because it is needed by the following behavior(s):";

                foreach (var behavior in behaviorNeedingState)
                {
                    message += "\n" + behavior.Name;
                }

                MessageBox.Show(message);
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new state name";
                tiw.Result = SelectedState.Self.SelectedStateSave.Name;
                var result = tiw.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string oldName = stateSave.Name;

                    stateSave.Name = tiw.Result;
                    GumCommands.Self.GuiCommands.RefreshStateTreeView();
                    // I don't think we need to save the project when renaming a state:
                    //GumCommands.Self.FileCommands.TryAutoSaveProject();

                    PluginManager.Self.StateRename(stateSave, oldName);

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                }
            }
        }
        #endregion

        #region Category

        public void RemoveStateCategory( StateSaveCategory category, IStateCategoryListContainer stateCategoryListContainer)
        {
            DeleteLogic.Self.RemoveStateCategory(category, stateCategoryListContainer);
        }

        public void AddCategory()
        {

            var target = SelectedState.Self.SelectedStateContainer as IStateCategoryListContainer;
            if (target == null)
            {
                MessageBox.Show("You must first select an element or behavior to add a state category");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new category name:";

                if (tiw.ShowDialog() == DialogResult.OK)
                {
                    string name = tiw.Result;

                    StateSaveCategory category = ElementCommands.Self.AddCategory(
                        target, name);

                    ElementTreeViewManager.Self.RefreshUi(SelectedState.Self.SelectedStateContainer);

                    StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedStateContainer);

                    SelectedState.Self.SelectedStateCategorySave = category;

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentObject();
                }
            }

        }

        internal void RenameStateCategory(StateSaveCategory category, ElementSave elementSave)
        {
            // This category can only be renamed if no behaviors require it
            var behaviorsNeedingCategory = DeleteLogic.Self.GetBehaviorsNeedingCategory(category, elementSave as ComponentSave);

            if (behaviorsNeedingCategory.Any())
            {
                string message =
                    "This category cannot be renamed because it is needed by the following behavior(s):";

                foreach (var behavior in behaviorsNeedingCategory)
                {
                    message += "\n" + behavior.Name;
                }

                MessageBox.Show(message);
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new category name";
                tiw.Result = category.Name;
                var result = tiw.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string oldName = category.Name;

                    category.Name = tiw.Result;
                    
                    GumCommands.Self.GuiCommands.RefreshStateTreeView();
                    // I don't think we need to save the project when renaming a state:
                    //GumCommands.Self.FileCommands.TryAutoSaveProject();

                    PluginManager.Self.CategoryRename(category, oldName);

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentObject();
                }
            }
        }

        #endregion

        #region Behavior

        private List<BehaviorSave> GetBehaviorsNeedingState(StateSave stateSave)
        {
            List<BehaviorSave> toReturn = new List<BehaviorSave>();
            // Try to get the parent container from the state...
            var element = stateSave.ParentContainer;
            if(element == null)
            {
                // ... if we can't find it for some reason, assume it's the current element (is this bad?)
                element = SelectedState.Self.SelectedElement;
            }

            var componentSave = element as ComponentSave;

            if(element != null)
            {
                // uncategorized states can't come from behaviors:
                bool isUncategorized = element.States.Contains(stateSave);
                StateSaveCategory elementCategory = null;

                if (!isUncategorized)
                {
                    elementCategory = element.Categories.FirstOrDefault(item => item.States.Contains(stateSave));
                }

                if (elementCategory != null)
                {
                    var allBehaviorsNeedingCategory = DeleteLogic.Self.GetBehaviorsNeedingCategory(elementCategory, componentSave);

                    foreach(var behavior in allBehaviorsNeedingCategory)
                    {
                        var behaviorCategory = behavior.Categories.First(item => item.Name == elementCategory.Name);

                        bool isStateReferencedInCategory = behaviorCategory.States.Any(item => item.Name == stateSave.Name);

                        if(isStateReferencedInCategory)
                        {
                            toReturn.Add(behavior);
                        }
                    }
                }
            }

            return toReturn;
        }

        public void RemoveBehaviorVariable(BehaviorSave container, VariableSave variable)
        {
            container.RequiredVariables.Variables.Remove(variable);
            GumCommands.Self.FileCommands.TryAutoSaveBehavior(container);
            PropertyGridManager.Self.RefreshUI();
        }

        public void AddBehavior()
            {
                if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
                {
                    MessageBox.Show("You must first save the project before adding a new component");
                }
                else
                {
                    TextInputWindow tiw = new TextInputWindow();
                    tiw.Message = "Enter new behavior name:";

                    if (tiw.ShowDialog() == DialogResult.OK)
                    {
                        string name = tiw.Result;

                        string whyNotValid;

                        NameVerifier.Self.IsBehaviorNameValid(name, null, out whyNotValid);

                        if (!string.IsNullOrEmpty(whyNotValid))
                        {
                            MessageBox.Show(whyNotValid);
                        }
                        else
                        {
                            var behavior = new BehaviorSave();
                            behavior.Name = name;

                            ProjectManager.Self.GumProjectSave.BehaviorReferences.Add(new BehaviorReference { Name = name });
                            ProjectManager.Self.GumProjectSave.BehaviorReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
                            ProjectManager.Self.GumProjectSave.Behaviors.Add(behavior);

                            GumCommands.Self.GuiCommands.RefreshElementTreeView();
                            SelectedState.Self.SelectedBehavior = behavior;

                            GumCommands.Self.FileCommands.TryAutoSaveProject();
                            GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
                        }
                    }
                }
            }

        #endregion

        #region Component


        public void DuplicateSelectedElement()
        {
            var element = SelectedState.Self.SelectedElement;

            if (element == null)
            {
                MessageBox.Show("You must first save the project before adding a new component");
            }
            else if (element is StandardElementSave)
            {
                MessageBox.Show("Standard Elements cannot be duplicated");
            }
            else if (element is ScreenSave)
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new Component name:";

                // todo - handle folders... do we support folders?

                tiw.Result = element.Name + "Copy";

                if (tiw.ShowDialog() == DialogResult.OK)
                {
                    string name = tiw.Result;

                    string whyNotValid;

                    NameVerifier.Self.IsScreenNameValid(tiw.Result, null, out whyNotValid);

                    if (string.IsNullOrEmpty(whyNotValid))
                    {
                        var newScreen = (element as ScreenSave).Clone();
                        newScreen.Name = name;
                        newScreen.Initialize(null);

                        ProjectCommands.Self.AddScreen(newScreen);
                    }
                    else
                    {
                        MessageBox.Show($"Invalid name for new screen: {whyNotValid}");
                    }
                }
            }
            else if (element is ComponentSave)
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new Component name:";

                FilePath filePath = element.Name;
                var nameWithoutPath = filePath.FileNameNoPath;

                string folder = null;
                if(element.Name.Contains("/"))
                {
                    folder = element.Name.Substring(0, element.Name.LastIndexOf('/'));
                }

                tiw.Result = nameWithoutPath + "Copy";

                if (tiw.ShowDialog() == DialogResult.OK)
                {
                    string name = tiw.Result;

                    string whyNotValid;
                    NameVerifier.Self.IsComponentNameValid(tiw.Result, folder, null, out whyNotValid);

                    if (string.IsNullOrEmpty(whyNotValid))
                    {
                        var newComponent = (element as ComponentSave).Clone();
                        if(!string.IsNullOrEmpty(folder))
                        {
                            folder += "/";
                        }
                        newComponent.Name = folder + name;
                        newComponent.Initialize(null);

                        ProjectCommands.Self.AddComponent(newComponent);
                    }
                    else
                    {
                        MessageBox.Show($"Invalid name for new component: {whyNotValid}");
                    }
                }
            }

        }

        #endregion

    }
}
