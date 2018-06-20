using System;
using System.Linq;
using System.Collections.Generic;
using STARS.Applications.Interfaces;
using STARS.Applications.VETS.Interfaces;
using STARS.Applications.Interfaces.Entities;
using STARS.Applications.Interfaces.Navigation;
using STARS.Applications.Interfaces.ViewModels;
using STARS.Applications.Interfaces.ViewModels.PropertyEditors;

namespace ToolsForReuse
{
    public static class SwapView
    {
        public static void Show<T>(string entityName = null) where T : Entity, new()
        {
            Show(ExtendedEntityManager.GetEntityTypeID<T>(), entityName);
        }

        public static void Show(string groupName, string entityName = null)
        {
            if (MEF.Application == null || MEF.Application.ExplorerGroupsManager == null) return;

            var groupsManager = MEF.Application.ExplorerGroupsManager.Value;
            if (groupsManager == null) return;

            //Left panel group items: { Resources, TestProcedures, Results, System, Data, TestExecution }
            var groups = ObjectExtensions.GetProperty<IEnumerable<Lazy<IItemContainerWithState<IReadOnlyDisplayItem>, IActiveItemMetadata>>>(groupsManager, "Items");
            if (groups == null) return;

            INavigableItem view = null;
            foreach (var group in groups)
            {   
                //Specific view as determined by Type <T> (i.e. Tests view)
                if (((IItemsCollection<Lazy<INavigableItem, IExplorerItemMetadata>>)group.Value.Item).Items.Any(x => x.Metadata.Uri.Contains(groupName)))
                {
                    view = ((IItemsCollection<Lazy<INavigableItem, IExplorerItemMetadata>>)group.Value.Item).Items.FirstOrDefault(x => x.Metadata.Uri.Contains(groupName)).Value;
                    break;
                }
            }
            if (view == null) return;

            if (entityName == null)
            {
                //No entity specified, load top level view (i.e. show all tests)
                MEF.Application.WorkspaceManager.Value.ActivateItem(view);
                return;
            }

            var entityView = ((IItemsCollection<Lazy<IEntityViewModel>>)view).Items.FirstOrDefault(x => x.Value.Name.Value == entityName);
            if (entityView == null) return;

            var selectedItem = ObjectExtensions.GetProperty<IObservableValue<Lazy<IEntityViewModel>>>(view, "SelectedItem");
            if (selectedItem == null) return;

            var commands = ObjectExtensions.GetProperty<ICommandViewModelManager>(view, "Commands");
            if (commands == null) return;

            var command = commands.Commands.FirstOrDefault(x => x.Value.DisplayName == "Edit").Value.Command;
            if (command == null) return;

            //Set the selected item to the entity having specified entityName and execute the 'edit' command
            selectedItem.Value = entityView;
            command.Execute(new object());
        }

    }
}
