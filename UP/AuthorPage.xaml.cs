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
    /// Логика взаимодействия для AuthorPage.xaml
    /// </summary>
    /// <remarks>
    /// Страница управления книгами автора. Доступна только пользователям с ролью "Автор" (RoleId = 2).
    /// 
    /// Функциональные возможности:
    /// - Просмотр активных книг (не замороженных)
    /// - Просмотр замороженных книг
    /// - Добавление новых книг (открывает окно AddEditBookWindow)
    /// - Редактирование существующих книг
    /// - Оспаривание заморозки книги (подача заявки на разморозку)
    /// - Переход на страницу детального просмотра книги
    /// 
    /// Страница имеет две вкладки:
    /// - "Активные книги" - показываются книги с IsFrozen = false
    /// - "Замороженные книги" - показываются книги с IsFrozen = true
    /// 
    /// Для замороженных книг доступна дополнительная кнопка "Оспорить",
    /// которая позволяет автору подать заявку на разморозку.
    /// </remarks>
    public partial class AuthorPage : Page
    {
        /// <summary>
        /// Конструктор страницы автора
        /// </summary>
        /// <remarks>
        /// Выполняет:
        /// 1. InitializeComponent() - загрузка XAML разметки
        /// 2. LoadBooks() - загрузка списка книг автора
        /// </remarks>
        public AuthorPage()
        {
            InitializeComponent();
            LoadBooks();
        }

        /// <summary>
        /// Загружает книги текущего автора из базы данных
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Проверяет, авторизован ли пользователь и имеет ли он роль "Автор" (RoleId = 2)
        /// 2. Если нет - показывает сообщение "Доступно только авторам"
        /// 3. Если да - загружает все книги текущего пользователя из таблицы Books
        /// 4. Присоединяет имя автора из таблицы Users
        /// 5. Разделяет книги на активные (IsFrozen = false) и замороженные (IsFrozen = true)
        /// 6. Отображает активные книги в ActiveBooksControl, замороженные - в FrozenBooksControl
        /// 7. Если книг нет - показывает сообщение "У вас нет книг"
        /// 
        /// Цветовая гамма соответствует основным цветам приложения:
        /// - Фон вкладок: #C2CDB2 (активная), #8A9A6E (неактивная)
        /// - Текст: #4A3424 (тёмно-коричневый)
        /// </remarks>
        private void LoadBooks()
        {
            if (!Session.IsAuthenticated || Session.CurrentUser.RoleId != 2)
            {
                EmptyText.Text = "Доступно только авторам";
                EmptyText.Visibility = Visibility.Visible;
                ActiveBooksControl.Visibility = Visibility.Collapsed;
                FrozenBooksControl.Visibility = Visibility.Collapsed;
                return;
            }

            var books = (from b in Core.Context.Books
                         join u in Core.Context.Users on b.UserId equals u.ID
                         where b.UserId == Session.CurrentUser.ID
                         select new AuthorBookViewModel
                         {
                             ID = b.ID,
                             BookName = b.BookName,
                             Image = b.Image,
                             AuthorName = u.UserName,
                             IsFrozen = b.IsFrozen
                         }).ToList();

            var activeBooks = books.Where(b => b.IsFrozen == false).ToList();
            var frozenBooks = books.Where(b => b.IsFrozen == true).ToList();

            ActiveBooksControl.ItemsSource = activeBooks;
            FrozenBooksControl.ItemsSource = frozenBooks;

            EmptyText.Visibility = (activeBooks.Count == 0 && frozenBooks.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Обработчик нажатия на вкладку "Активные книги"
        /// </summary>
        /// <remarks>
        /// Переключает отображение на список активных книг.
        /// Визуальное оформление:
        /// - Активная вкладка: фон #C2CDB2, текст #4A3424
        /// - Неактивная вкладка: фон #8A9A6E, текст White
        /// </remarks>
        private void ActiveTab_Click(object sender, RoutedEventArgs e)
        {
            ActiveTab.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#C2CDB2");
            ActiveTab.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#4A3424");
            FrozenTab.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#8A9A6E");
            FrozenTab.Foreground = Brushes.White;
            ActiveBooksControl.Visibility = Visibility.Visible;
            FrozenBooksControl.Visibility = Visibility.Collapsed;
            EmptyText.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Обработчик нажатия на вкладку "Замороженные книги"
        /// </summary>
        /// <remarks>
        /// Переключает отображение на список замороженных книг.
        /// Визуальное оформление:
        /// - Активная вкладка: фон #C2CDB2, текст #4A3424
        /// - Неактивная вкладка: фон #8A9A6E, текст White
        /// </remarks>
        private void FrozenTab_Click(object sender, RoutedEventArgs e)
        {
            FrozenTab.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#C2CDB2");
            FrozenTab.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#4A3424");
            ActiveTab.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#8A9A6E");
            ActiveTab.Foreground = Brushes.White;
            FrozenBooksControl.Visibility = Visibility.Visible;
            ActiveBooksControl.Visibility = Visibility.Collapsed;
            EmptyText.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Обработчик клика по карточке книги
        /// </summary>
        /// <remarks>
        /// При клике на карточку открывает страницу детального просмотра книги (BookWindow).
        /// ID книги передаётся через свойство Tag элемента Border.
        /// </remarks>
        private void BookCard_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null)
            {
                int bookId = (int)border.Tag;
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.MainFrame.Navigate(new BookWindow(bookId));
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки "Добавить книгу"
        /// </summary>
        /// <remarks>
        /// Открывает окно AddEditBookWindow в режиме добавления новой книги.
        /// После закрытия окна обновляет список книг (LoadBooks).
        /// </remarks>
        private void AddBookBtn_Click(object sender, RoutedEventArgs e)
        {
            AddEditBookWindow addWindow = new AddEditBookWindow();
            addWindow.ShowDialog();
            LoadBooks();
        }

        /// <summary>
        /// Обработчик кнопки "Редактировать"
        /// </summary>
        /// <remarks>
        /// Открывает окно AddEditBookWindow в режиме редактирования существующей книги.
        /// ID редактируемой книги передаётся в конструктор окна.
        /// После закрытия окна обновляет список книг (LoadBooks).
        /// </remarks>
        private void EditBookBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int bookId = (int)btn.Tag;
            AddEditBookWindow editWindow = new AddEditBookWindow(bookId);
            editWindow.ShowDialog();
            LoadBooks();
        }

        /// <summary>
        /// Обработчик кнопки "Оспорить" (только для замороженных книг)
        /// </summary>
        /// <remarks>
        /// Позволяет автору подать заявку на разморозку книги.
        /// Алгоритм:
        /// 1. Открывает окно AppealWindow для ввода причины
        /// 2. Если причина указана - создаёт запись в таблице UnfreezeApplications
        /// 3. Указывает TargetTypeId = 2 (Book)
        /// 4. Сохраняет заявку со статусом "В ожидании"
        /// 5. Показывает сообщение об успехе
        /// 6. Обновляет список книг (LoadBooks)
        /// 
        /// Заявка будет рассмотрена администратором на странице AdminPage.
        /// Доступно только для замороженных книг (кнопка отображается только на вкладке "Замороженные книги").
        /// </remarks>
        private void AppealBookBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int bookId = (int)btn.Tag;

            AppealWindow appealWindow = new AppealWindow("Оспаривание заморозки книги");
            appealWindow.ShowDialog();

            if (appealWindow.IsSubmitted)
            {
                var appeal = new UnfreezeApplications
                {
                    UserId = Session.CurrentUser.ID,
                    TargetTypeId = 2,
                    TargetBookId = bookId,
                    Reason = appealWindow.Reason,
                    ApplicationDate = DateTime.Now,
                    Status = "В ожидании"
                };
                Core.Context.UnfreezeApplications.Add(appeal);
                Core.Context.SaveChanges();
                MessageBox.Show("Заявка на разморозку книги подана!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadBooks();
            }
        }
    }

    /// <summary>
    /// Модель представления для книги на странице автора
    /// </summary>
    /// <remarks>
    /// Используется для отображения книг в списках ActiveBooksControl и FrozenBooksControl.
    /// Содержит минимальный набор полей, необходимых для отображения карточки книги:
    /// - ID: идентификатор книги (для операций редактирования и навигации)
    /// - BookName: название книги
    /// - Image: путь к обложке книги
    /// - AuthorName: имя автора
    /// - IsFrozen: статус заморозки (определяет, в каком списке отображать)
    /// 
    /// В отличие от BookViewModel, не содержит рейтинга, описания и текста,
    /// так как эти данные не нужны на странице автора.
    /// </remarks>
    public class AuthorBookViewModel
    {
        public int ID { get; set; }
        public string BookName { get; set; }
        public string Image { get; set; }
        public string AuthorName { get; set; }
        public bool IsFrozen { get; set; }
    }
}