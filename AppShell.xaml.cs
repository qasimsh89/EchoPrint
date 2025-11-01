using System.Threading.Tasks;


namespace ECHO_PRINT
{
    public partial class AppShell : Shell
    {
        private bool RequestNotifications;
        public AppShell()
        {
            InitializeComponent();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();


                if (RequestNotifications) return;
                    RequestNotifications = true;
                    

        }
    }
}
