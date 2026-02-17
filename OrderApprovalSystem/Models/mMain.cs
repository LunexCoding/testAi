using Fox;


namespace OrderApprovalSystem.Models
{

    public class mMain : MBase
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        public mMain()
        {
            pCurrentViewName = "Dev"; // Начальное представление
        }

        #region Свойства

        /// <summary>
        /// Имя текущего представления
        /// </summary>
        public string pCurrentViewName
        {
            get { return _currentViewName; }
            set
            {
                _currentViewName = value;
                OnPropertyChanged("pCurrentViewName");
            }
        }

        #endregion Свойства

        #region Переменные

        /// <summary>
        /// Текущее активное представление
        /// </summary>
        private string _currentViewName;

        #endregion Переменные
    }

}
