using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

using Segway.Modules.ShellControls;


namespace Segway.Modules.ListBox_Item_Def
{
    public class List_Box_Item_Definition : INotifyPropertyChanged
    {
        public String Title { get; set; }
        public ImageSource Image { get; set; }
        public UserControl Content { get; set; }
        public IViewModel ViewModel { get; set; }
        public Boolean IsDefinitionEnabled
        {
            get
            {
                return _IsDefinitionEnabled;
            }
            set
            {
                _IsDefinitionEnabled = value;
                OnPropertyChanged("IsDefinitionEnabled");
            }
        }

        private Boolean _IsDefinitionEnabled = false;

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
            IsDefinitionEnabled = enabled;
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
