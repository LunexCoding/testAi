using System.Xml.Serialization;


namespace OrderApprovalSystem.Core.Settings
{

    public class AppSettings : Fox.Core.Settings.Settings
    {
        [XmlElement("Theme")]
        public string Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _theme = "Default";
    }

}
