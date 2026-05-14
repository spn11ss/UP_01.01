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
    /// Логика взаимодействия для ReadingListsPage.xaml
    /// </summary>
    /// <remarks>
    /// Страница управления личными списками чтения пользователя.
    /// Позволяет просматривать книги в четырёх категориях:
    /// - В планах (statusId = 1)
    /// - Читаю (statusId = 2)
    /// - Прочитано (statusId = 3)
    /// - Заброшено (statusId = 4)
    /// 
    /// Функциональные возможности:
    /// - Переключение между списками через вкладки
    /// - Поиск по названию или автору
    /// - Сортировка по названию или рейтингу
    /// - Фильтрация по жанрам
    /// - Перемещение книг между списками (изменение статуса)
    /// - Переход на страницу детального просмотра книги
    /// 
    /// При загрузке страницы отображаются книги из списка "В планах" (по умолчанию).
    /// </remarks>
    public partial class ReadingListsPage : Page
    {
        /// <summary>
        /// ID текущего выбранного статуса (1-4)
        /// </summary>
        /// <remarks>
        /// По умолчанию = 1 (список "В планах")
        /// </remarks>
        private int currentStatusId = 1;

        /// <summary>
        /// Список книг в текущем выбранном списке
        /// </summary>
        private List<BookViewModel> currentBooks;

        /// <summary>
        /// Список всех жанров из базы данных
        /// </summary>
        private List<Genres> allGenres;

        /// <summary>
        /// Флаг, указывающий на завершение загрузки данных
        /// </summary>
        private bool isLoaded = false;

        /// <summary>
        /// Конструктор страницы списков книг
        /// </summary>
        /// <remarks>
        /// Выполняет последовательную инициализацию:
        /// 1. InitializeComponent() - загрузка XAML разметки
        /// 2. LoadGenres() - заполнение выпадающего списка жанров
        /// 3. LoadBooks() - загрузка книг текущего пользователя для статуса "В планах"
        /// 4. UpdateTabStyles() - визуальное выделение активной вкладки
        /// </remarks>
        public ReadingListsPage()
        {
            InitializeComponent();
            LoadGenres();
            LoadBooks();
            UpdateTabStyles();
        }

        /// <summary>
        /// Загружает список жанров из базы данных
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Получает все жанры из таблицы Genres
        /// 2. Очищает выпадающий список GenreFilterBox
        /// 3. Добавляет пункт "Все жанры" (Tag = null) - отключает фильтрацию
        /// 4. Добавляет каждый жанр как ComboBoxItem с Tag = ID жанра
        /// 5. Устанавливает выбранный элемент по умолчанию - "Все жанры"
        /// </remarks>
        private void LoadGenres()
        {
            allGenres = Core.Context.Genres.ToList();
            GenreFilterBox.Items.Clear();
            GenreFilterBox.Items.Add(new ComboBoxItem { Content = "Все жанры", Tag = null });

            foreach (var genre in allGenres)
            {
                GenreFilterBox.Items.Add(new ComboBoxItem { Content = genre.GenreName, Tag = genre.ID });
            }
            GenreFilterBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Загружает книги текущего пользователя для выбранного статуса
        /// </summary>
        /// <remarks>
        /// Формирует запрос к базе данных:
        /// 1. Выбирает записи из ReadingLists для текущего пользователя и выбранного статуса
        /// 2. Присоединяет информацию о книге (Books) и авторе (Users)
        /// 3. Отображает только незамороженные книги (IsFrozen == false)
        /// 4. Вычисляет средний рейтинг по незамороженным отзывам
        /// 
        /// Если пользователь не авторизован - отображает пустой список с сообщением.
        /// После загрузки устанавливается флаг isLoaded = true и вызывается ApplyFilters().
        /// </remarks>
        private void LoadBooks()
        {
            if (!Session.IsAuthenticated)
            {
                BooksItemsControl.ItemsSource = null;
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var query = from rl in Core.Context.ReadingLists
                            join b in Core.Context.Books on rl.BookId equals b.ID
                            join u in Core.Context.Users on b.UserId equals u.ID
                            where rl.UserId == Session.CurrentUser.ID && rl.BookStatusId == currentStatusId && b.IsFrozen == false
                            select new BookViewModel
                            {
                                ID = b.ID,
                                BookName = b.BookName,
                                Description = b.Description,
                                Image = b.Image,
                                AuthorName = u.UserName,
                                IsFrozen = b.IsFrozen,
                                AvgRating = (double?)b.Reviews.Where(r => r.IsFrozen == false).Average(r => r.Rating) ?? 0,
                                ReviewsCount = b.Reviews.Count(r => r.IsFrozen == false),
                                ReadingListId = rl.ID,
                                CurrentStatusId = rl.BookStatusId
                            };

                currentBooks = query.ToList();
                isLoaded = true;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Применяет текущие фильтры и сортировку к списку книг
        /// </summary>
        /// <remarks>
        /// Этапы обработки:
        /// 1. Проверка готовности данных (isLoaded и currentBooks)
        /// 2. Копирование списка для фильтрации
        /// 3. Поиск - фильтрация по совпадению с названием или автором
        /// 4. Фильтр по жанру - оставляет только книги с выбранным жанром
        /// 5. Сортировка - применяет выбранный порядок
        /// 6. Отображение результата в BooksItemsControl
        /// 7. Управление видимостью EmptyText
        /// 
        /// Вызывается при изменении текста поиска, выборе сортировки или фильтра.
        /// </remarks>
        private void ApplyFilters()
        {
            if (!isLoaded || currentBooks == null) return;

            var filtered = currentBooks.AsEnumerable();

            string search = SearchBox.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(b =>
                    b.BookName.ToLower().Contains(search) ||
                    b.AuthorName.ToLower().Contains(search));
            }

            if (GenreFilterBox != null && GenreFilterBox.SelectedItem != null)
            {
                var selectedGenre = GenreFilterBox.SelectedItem as ComboBoxItem;
                if (selectedGenre?.Tag != null)
                {
                    int genreId = (int)selectedGenre.Tag;
                    var booksWithGenre = Core.Context.BooksGenres
                        .Where(bg => bg.GenreId == genreId)
                        .Select(bg => bg.BookId)
                        .ToList();
                    filtered = filtered.Where(b => booksWithGenre.Contains(b.ID));
                }
            }

            if (SortBox != null && SortBox.SelectedItem != null)
            {
                string sort = (SortBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                switch (sort)
                {
                    case "По названию ↑":
                        filtered = filtered.OrderBy(b => b.BookName);
                        break;
                    case "По названию ↓":
                        filtered = filtered.OrderByDescending(b => b.BookName);
                        break;
                    case "По рейтингу ↑":
                        filtered = filtered.OrderBy(b => b.AvgRating);
                        break;
                    case "По рейтингу ↓":
                        filtered = filtered.OrderByDescending(b => b.AvgRating);
                        break;
                    default:
                        filtered = filtered.OrderBy(b => b.BookName);
                        break;
                }
            }

            var result = filtered.ToList();
            BooksItemsControl.ItemsSource = result;
            EmptyText.Visibility = result.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Обновляет визуальное оформление вкладок
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Устанавливает цвет текста SaddleBrown для всех вкладок (сброс)
        /// 2. Устанавливает цвет текста DarkOliveGreen для активной вкладки
        /// 
        /// Цвета выбраны в соответствии с цветовой гаммой приложения:
        /// - SaddleBrown (коричневый) - неактивные вкладки
        /// - DarkOliveGreen (оливково-зелёный) - активная вкладка
        /// </remarks>
        private void UpdateTabStyles()
        {

            PlanTab.Foreground = Brushes.SaddleBrown;
            ReadingTab.Foreground = Brushes.SaddleBrown;
            ReadTab.Foreground = Brushes.SaddleBrown;
            AbandonedTab.Foreground = Brushes.SaddleBrown;

            if (currentStatusId == 1) PlanTab.Foreground = Brushes.DarkOliveGreen;
            else if (currentStatusId == 2) ReadingTab.Foreground = Brushes.DarkOliveGreen;
            else if (currentStatusId == 3) ReadTab.Foreground = Brushes.DarkOliveGreen;
            else if (currentStatusId == 4) AbandonedTab.Foreground = Brushes.DarkOliveGreen;
        }

        /// <summary>
        /// Загружает книги для указанного статуса и обновляет интерфейс
        /// </summary>
        /// <param name="statusId">ID статуса (1-4)</param>
        private void LoadBooksByStatus(int statusId)
        {
            currentStatusId = statusId;
            UpdateTabStyles();
            LoadBooks();
        }

        /// <summary>
        /// Обработчик нажатия на вкладку "В планах"
        /// </summary>
        private void PlanTab_Click(object sender, RoutedEventArgs e)
        {
            LoadBooksByStatus(1);
        }

        /// <summary>
        /// Обработчик нажатия на вкладку "Читаю"
        /// </summary>
        private void ReadingTab_Click(object sender, RoutedEventArgs e)
        {
            LoadBooksByStatus(2);
        }

        /// <summary>
        /// Обработчик нажатия на вкладку "Прочитано"
        /// </summary>
        private void ReadTab_Click(object sender, RoutedEventArgs e)
        {
            LoadBooksByStatus(3);
        }

        /// <summary>
        /// Обработчик нажатия на вкладку "Заброшено"
        /// </summary>
        private void AbandonedTab_Click(object sender, RoutedEventArgs e)
        {
            LoadBooksByStatus(4);
        }

        /// <summary>
        /// Обработчик изменения текста в поле поиска
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Обработчик изменения выбора в выпадающем списке сортировки
        /// </summary>
        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Обработчик изменения выбора в выпадающем списке жанров
        /// </summary>
        private void GenreFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Обработчик нажатия кнопки сброса фильтров
        /// </summary>
        /// <remarks>
        /// Сбрасывает:
        /// - Текст поиска
        /// - Сортировку на значение по умолчанию (индекс 0)
        /// - Фильтр жанров на "Все жанры" (индекс 0)
        /// После сброса обновляет отображение книг
        /// </remarks>
        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            if (SortBox != null) SortBox.SelectedIndex = 0;
            if (GenreFilterBox != null) GenreFilterBox.SelectedIndex = 0;
            ApplyFilters();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Переместить"
        /// </summary>
        /// <remarks>
        /// Позволяет изменить статус книги (переместить между списками):
        /// 1. Находит запись в ReadingLists для текущего пользователя и книги
        /// 2. Открывает окно MoveStatusWindow для выбора нового статуса
        /// 3. После закрытия окна обновляет список книг
        /// </remarks>
        private void MoveBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int bookId = (int)btn.Tag;

            var currentEntry = Core.Context.ReadingLists
                .FirstOrDefault(rl => rl.UserId == Session.CurrentUser.ID && rl.BookId == bookId);

            if (currentEntry == null) return;

            MoveStatusWindow moveWindow = new MoveStatusWindow(bookId, currentEntry.ID, currentStatusId);
            moveWindow.ShowDialog();
            LoadBooks();
        }

        /// <summary>
        /// Обработчик клика по карточке книги
        /// </summary>
        /// <remarks>
        /// При клике открывает страницу детального просмотра книги (BookWindow)
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
    }
}