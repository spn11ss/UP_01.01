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
    /// Логика взаимодействия для CatalogPage.xaml
    /// </summary>
    /// <remarks>
    /// Страница каталога книг, отображает все книги в виде сетки карточек.
    /// Предоставляет функционал:
    /// - Поиск книг по названию или автору
    /// - Сортировка по названию (возрастание/убывание) и по рейтингу (возрастание/убывание)
    /// - Фильтрация по жанрам (одиночный выбор)
    /// - Добавление книг в пользовательские списки (В планах/Читаю/Прочитано/Заброшено)
    /// - Переход на страницу детального просмотра книги по клику на карточку
    /// 
    /// Данные загружаются из базы при инициализации страницы.
    /// Применение фильтров происходит мгновенно при изменении любого параметра.
    /// </remarks>
    public partial class CatalogPage : Page
    {
        /// <summary>
        /// Полный список всех книг (нефильтрованный)
        /// Список всех жанров из базы данных
        /// </summary>
        private List<BookViewModel> allBooks;
        private List<Genres> allGenres;

        /// <summary>
        /// Флаг, указывающий на завершение начальной загрузки данных
        /// </summary>
        /// <remarks>
        /// Используется для защиты от вызова ApplyFilters() до того, как allBooks будет заполнен.
        /// Предотвращает ошибки NullReferenceException при инициализации.
        /// </remarks>
        private bool isLoaded = false;

        /// <summary>
        /// Конструктор страницы каталога
        /// </summary>
        /// <remarks>
        /// Выполняет последовательную инициализацию:
        /// 1. InitializeComponent() - загрузка XAML разметки
        /// 2. LoadGenres() - заполнение выпадающего списка жанров
        /// 3. LoadBooks() - загрузка списка книг из базы данных
        /// </remarks>
        public CatalogPage()
        {
            InitializeComponent();
            LoadGenres();
            LoadBooks();
        }

        /// <summary>
        /// Загружает список жанров из базы данных в выпадающий список
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Получает все жанры из таблицы Genres
        /// 2. Очищает существующие элементы в GenreFilterBox
        /// 3. Добавляет пункт "Все жанры" (Tag = null) - отключает фильтрацию
        /// 4. Добавляет каждый жанр как ComboBoxItem, где Tag = ID жанра
        /// 5. Устанавливает выбранный по умолчанию пункт "Все жанры" (SelectedIndex = 0)
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
        /// Загружает список книг из базы данных
        /// </summary>
        /// <remarks>
        /// Формирует запрос, который:
        /// 1. Выбирает только незамороженные книги (IsFrozen == false)
        /// 2. Объединяет таблицы Books и Users для получения имени автора
        /// 3. Вычисляет средний рейтинг книги по незамороженным отзывам
        /// 4. Подсчитывает количество отзывов
        /// 
        /// В случае ошибки при загрузке выводится сообщение пользователю.
        /// После успешной загрузки устанавливается флаг isLoaded = true и вызывается ApplyFilters().
        /// </remarks>
        private void LoadBooks()
        {
            try
            {
                var query = from b in Core.Context.Books
                            join u in Core.Context.Users on b.UserId equals u.ID
                            where b.IsFrozen == false
                            select new BookViewModel
                            {
                                ID = b.ID,
                                BookName = b.BookName,
                                Description = b.Description,
                                Image = b.Image,
                                AuthorName = u.UserName,
                                IsFrozen = b.IsFrozen,
                                AvgRating = (double?)b.Reviews.Where(r => r.IsFrozen == false).Average(r => r.Rating) ?? 0,
                                ReviewsCount = b.Reviews.Count(r => r.IsFrozen == false)
                            };

                allBooks = query.ToList();
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
        /// Применяет текущие фильтры, сортировку и поиск к списку книг
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Проверяет, что данные загружены (isLoaded и allBooks не null)
        /// 2. Создаёт копию списка для фильтрации
        /// 3. Поиск: фильтрует по вхождению строки в название или имя автора (без учёта регистра)
        /// 4. Фильтр по жанру: оставляет только книги, содержащие выбранный жанр
        /// 5. Сортировка: применяет выбранный порядок сортировки
        /// 6. Отображает результат в BooksItemsControl
        /// 7. Показывает EmptyText, если результат пуст
        /// 
        /// Обработчики SearchBox_TextChanged, SortBox_SelectionChanged, GenreFilterBox_SelectionChanged
        /// вызывают этот метод для обновления отображения.
        /// </remarks>
        private void ApplyFilters()
        {
            if (!isLoaded || allBooks == null) return; 

            var filtered = allBooks.AsEnumerable();


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
        /// Обработчик изменения текста в поле поиска
        /// </summary>
        /// <param name="sender">TextBox SearchBox</param>
        /// <param name="e">Параметры события изменения текста</param>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Обработчик изменения выбранного элемента в выпадающем списке сортировки
        /// </summary>
        /// <param name="sender">ComboBox SortBox</param>
        /// <param name="e">Параметры события изменения выбора</param>
        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Обработчик изменения выбранного элемента в выпадающем списке жанров
        /// </summary>
        /// <param name="sender">ComboBox GenreFilterBox</param>
        /// <param name="e">Параметры события изменения выбора</param>
        private void GenreFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Обработчик нажатия кнопки сброса всех фильтров
        /// </summary>
        /// <remarks>
        /// Выполняет:
        /// 1. Очищает поле поиска
        /// 2. Устанавливает сортировку в значение по умолчанию (индекс 0)
        /// 3. Устанавливает фильтр жанров в "Все жанры" (индекс 0)
        /// 4. Вызывает ApplyFilters() для обновления отображения
        /// </remarks>
        /// <param name="sender">Button ResetBtn</param>
        /// <param name="e">Параметры события нажатия кнопки</param>
        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            if (SortBox != null) SortBox.SelectedIndex = 0;
            if (GenreFilterBox != null) GenreFilterBox.SelectedIndex = 0;
            ApplyFilters();
        }

        /// <summary>
        /// Обработчик клика по карточке книги
        /// </summary>
        /// <remarks>
        /// При клике на карточку книги:
        /// 1. Извлекает ID книги из свойства Tag
        /// 2. Находит главное окно приложения (MainWindow)
        /// 3. Навигирует на страницу детального просмотра книги (BookWindow)
        /// 
        /// Использует свойство Tag для передачи ID книги через элемент Border.
        /// </remarks>
        /// <param name="sender">Border - контейнер карточки книги</param>
        /// <param name="e">Параметры события клика мыши</param>
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
        /// Обработчик нажатия кнопки "В список"
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Проверяет, авторизован ли пользователь
        /// 2. Проверяет, не добавлена ли уже эта книга в списки пользователя
        /// 3. Открывает окно SelectStatusWindow для выбора статуса (В планах/Читаю/Прочитано/Заброшено)
        /// 4. После закрытия окна обновляет список книг
        /// 
        /// Если пользователь не авторизован - выводит предупреждение.
        /// Если книга уже в списке - выводит информационное сообщение.
        /// </remarks>
        /// <param name="sender">Button с текстом "В список"</param>
        /// <param name="e">Параметры события нажатия кнопки</param>
        private void AddToListBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int bookId = (int)btn.Tag;

            if (!Session.IsAuthenticated)
            {
                MessageBox.Show("Войдите в систему, чтобы добавлять книги в списки",
                    "Не авторизован", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = Core.Context.ReadingLists
                .FirstOrDefault(rl => rl.UserId == Session.CurrentUser.ID && rl.BookId == bookId);

            if (existing != null)
            {
                MessageBox.Show("Эта книга уже есть в вашем списке", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectStatusWindow statusWindow = new SelectStatusWindow(bookId);
            statusWindow.ShowDialog();
            LoadBooks(); 
        }
    }
}
