using CommunityToolkit.Mvvm.ComponentModel;
using Oblivion.Data.Entities;
using System.Collections.ObjectModel;
using System.Linq;

namespace Oblivion.GUI.MVVM.Model
{
    public partial class WorkspaceUIModel : ObservableObject
    {
        public Workspace Model { get; }
        public ObservableCollection<FileUIModel> Files { get; }

        public string Name
        {
            get => Model.Name;
            set
            {
                if (Model.Name != value)
                {
                    Model.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public WorkspaceUIModel(Workspace workspace)
        {
            Model = workspace;
            Files = new ObservableCollection<FileUIModel>(workspace.Files.Select(f => new FileUIModel(f)));
        }
    }
}
