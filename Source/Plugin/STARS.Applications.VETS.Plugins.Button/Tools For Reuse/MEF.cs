using Stars.ApplicationManager;
using Stars.DataDistribution;
using Stars.Resources;
using STARS.Applications.Interfaces.Dialogs;
using STARS.Applications.Interfaces.EntityManager;
using STARS.Applications.Interfaces.ViewModels;
using STARS.Applications.VETS.Interfaces;
using STARS.Applications.VETS.Interfaces.Logging;
using System.ComponentModel.Composition;

namespace ToolsForReuse
{
    [Export("RDEImportTool")]
    public class MEF
    {
        public static ISystemLogManager Logger { get; private set; }
        public static IApplication Application { get; private set; }
        public static IEntityQuery EntityQuery { get; private set; }
        public static IEntityCreate EntityCreate { get; private set; }
        public static ILiveResource LiveResources { get; private set; }
        public static IProvideValues ProvideValues { get; private set; }
        public static IDialogService DialogService { get; private set; }
        public static IStarsApplication StarsApplication { get; private set; }
        public static IVETSEntityManagerView EntityManagerView { get; private set; }

        [ImportingConstructor]
        public MEF
        (
            ISystemLogManager logger,
            IApplication application,
            IEntityQuery entityQuery,
            IEntityCreate entityCreate,
            ILiveResource liveResources,
            IProvideValues provideValues,
            IDialogService dialogService,
            IStarsApplication starsApplication,
            IVETSEntityManagerView entityManagerView       
        )
        {
            Logger = logger;
            Application = application;
            EntityQuery = entityQuery;
            EntityCreate = entityCreate;
            LiveResources = liveResources;
            ProvideValues = provideValues;
            DialogService = dialogService;
            StarsApplication = starsApplication;
            EntityManagerView = entityManagerView;
        }

        public MEF()
        {

        }
    }
}
