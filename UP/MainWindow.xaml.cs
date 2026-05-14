using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UP
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// <remarks>
    /// Главное окно приложения, управляет навигацией между страницами через боковое меню.
    /// При загрузке настраивает видимость кнопок в зависимости от роли пользователя (админ/автор/читатель)
    /// и открывает страницу каталога книг по умолчанию.
    /// </remarks>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Конструктор главного окна. Инициализирует компоненты, настраивает интерфейс и загружает стартовую страницу.
        /// </summary>
        /// <remarks>
        /// Выполняет следующие действия:
        /// 1. InitializeComponent() - загружает XAML разметку
        /// 2. Устанавливает текущее окно как главное в приложении
        /// 3. Показывает кнопку админа, если пользователь вошёл как администратор (Session.IsAdmin)
        /// 4. Показывает кнопку автора, если пользователь вошёл как автор (Session.IsAuthor)
        /// 5. Переходит на страницу каталога книг (CatalogPage)
        /// </remarks>
        public MainWindow()
        {
            InitializeComponent();

            Application.Current.MainWindow = this;

            if (Session.IsAdmin)
                AdminBtn.Visibility = Visibility.Visible;

            if (Session.IsAuthor)
                AuthorBtn.Visibility = Visibility.Visible;

            MainFrame.Navigate(new CatalogPage());
        }

        /// <summary>
        /// Обработчик кнопки "Каталог книг". Открывает страницу со списком всех книг.
        /// </summary>
        /// <param name="sender">Кнопка CatalogBtn</param>
        /// <param name="e">Параметры события клика</param>
        private void CatalogBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CatalogPage());
        }

        /// <summary>
        /// Обработчик кнопки "Списки книг". Открывает страницу с пользовательскими списками (В планах, Читаю, Прочитано, Заброшено).
        /// </summary>
        /// <param name="sender">Кнопка ListsBtn</param>
        /// <param name="e">Параметры события клика</param>
        private void ListsBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReadingListsPage());
        }

        /// <summary>
        /// Обработчик кнопки "Администрирование". Открывает панель управления (доступно только админам).
        /// </summary>
        /// <remarks>
        /// Кнопка видна только если Session.IsAdmin == true.
        /// На странице AdminPage находятся: список жалоб, список заявок на снятие заморозки, список заявок на получение роли админ
        /// список замороженных (книг, пользователей, отзывов), список пользователей с возможностью назначить роль и сменить пароль.
        /// </remarks>
        /// <param name="sender">Кнопка AdminBtn</param>
        /// <param name="e">Параметры события клика</param>
        private void AdminBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AdminPage());
        }

        /// <summary>
        /// Обработчик кнопки "Страница автора". Открывает страницу с книгами текущего автора (доступно только авторам).
        /// </summary>
        /// <remarks>
        /// Кнопка видна только если Session.IsAuthor == true.
        /// На странице AuthorPage автор может управлять своими книгами, просматривать замороженные книги и оспаривать заморозку.
        /// </remarks>
        /// <param name="sender">Кнопка AuthorBtn</param>
        /// <param name="e">Параметры события клика</param>
        private void AuthorBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AuthorPage());
        }


        /// <summary>
        /// Обработчик кнопки "Профиль". Открывает страницу личного кабинета пользователя.
        /// </summary>
        /// <remarks>
        /// На странице ProfilePage отображается: информация о пользователе, все оставленные 
        /// пользователем отзывы, подача заявки на роль автора, предупреждение о заморозке аккаунта 
        /// с указанием причины и возможностью оспорить заморозку.
        /// </remarks>
        /// <param name="sender">Кнопка ProfileBtn</param>
        /// <param name="e">Параметры события клика</param>
        private void ProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProfilePage());
        }
    }
}
