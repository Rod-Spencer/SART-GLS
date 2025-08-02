using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using Segway.Modules.ShellControls;

namespace Segway.Modules.CU_Log_Module
{
    public class List_Box_Item_Definition : INotifyPropertyChanged
    {
        public String Title { get; set; }
        public ImageSource Image { get; set; }
        public UserControl Content { get; set; }
        public IViewModel ViewModel { get; set; }
        public Boolean IsEnabled
        {
            get
            {
                return _IsEnabled;
            }
            set
            {
                _IsEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        private Boolean _IsEnabled = false;

        #region Constructors

        public List_Box_Item_Definition() { }

        public List_Box_Item_Definition(String title, ImageSource img, UserControl content)
        {
            Title = title;
            Image = img;
            Content = content;
            ViewModel = ((IView)content).ViewModel;
        }

        public List_Box_Item_Definition(String title, ImageSource img, IViewModel vm, Boolean enabled)
        {
            Title = title;
            Image = img;
            Content = (UserControl)vm.View;
            ViewModel = vm;
            IsEnabled = enabled;
        }

        #endregion


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(String prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        #endregion
    }
}
