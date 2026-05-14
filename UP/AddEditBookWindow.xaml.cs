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
using System.Windows.Shapes;

namespace UP
{
    /// <summary>
    /// Логика взаимодействия для AddEditBookWindow.xaml
    /// </summary>
    /// <remarks>
    /// Универсальное окно для добавления новой книги и редактирования существующей.
    /// Доступно только авторам (вызывается из AuthorPage).
    /// 
    /// Режимы работы:
    /// - Добавление (bookId = null): создание новой книги
    /// - Редактирование (bookId указан): изменение существующей книги
    /// 
    /// Функциональные возможности:
    /// - Заполнение названия, описания и текста книги
    /// - Выбор нескольких жанров (множественный выбор через CheckBox)
    /// - Указание пути к обложке (необязательно)
    /// - Валидация всех обязательных полей
    /// - Автоматическая привязка к текущему автору (UserId из Session)
    /// </remarks>
    public partial class AddEditBookWindow : Window
    {
        /// <summary>
        /// ID редактируемой книги (null при добавлении новой)
        /// </summary>
        private int? editBookId = null;

        /// <summary>
        /// Список жанров с флагами выбора
        /// </summary>
        private List<GenreCheckBox> genres;

        /// <summary>
        /// Конструктор окна добавления/редактирования книги
        /// </summary>
        /// <param name="bookId">ID книги для редактирования (null - добавление новой)</param>
        /// <remarks>
        /// Выполняет:
        /// 1. InitializeComponent() - загрузка XAML разметки
        /// 2. LoadGenres() - загрузка списка жанров из базы
        /// 3. Если передан bookId - устанавливает режим редактирования:
        ///    - Меняет заголовок на "Редактирование книги"
        ///    - Загружает данные книги (LoadBookData)
        /// </remarks>
        public AddEditBookWindow(int? bookId = null)
        {
            InitializeComponent();
            LoadGenres();

            if (bookId.HasValue)
            {
                editBookId = bookId;
                TitleText.Text = "Редактирование книги";
                LoadBookData();
            }
        }

        /// <summary>
        /// Загружает список жанров из базы данных
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Получает все жанры из таблицы Genres
        /// 2. Создаёт список GenreCheckBox с IsSelected = false
        /// 3. Устанавливает список как ItemsSource для GenresListBox
        /// 
        /// Каждый жанр отображается в виде CheckBox с названием жанра.
        /// Пользователь может выбрать несколько жанров (множественный выбор).
        /// </remarks>
        private void LoadGenres()
        {
            var allGenres = Core.Context.Genres.ToList();
            genres = allGenres.Select(g => new GenreCheckBox
            {
                GenreId = g.ID,
                GenreName = g.GenreName,
                IsSelected = false
            }).ToList();

            GenresListBox.ItemsSource = genres;
        }

        /// <summary>
        /// Загружает данные редактируемой книги
        /// </summary>
        /// <remarks>
        /// Выполняет:
        /// 1. Находит книгу по ID в таблице Books
        /// 2. Заполняет поля ввода (название, описание, текст, путь к обложке)
        /// 3. Отмечает жанры, уже привязанные к книге:
        ///    - Получает список GenreId из таблицы BooksGenres
        ///    - Устанавливает IsSelected = true для соответствующих жанров
        /// 4. Обновляет отображение списка жанров
        /// </remarks>
        private void LoadBookData()
        {
            var book = Core.Context.Books.FirstOrDefault(b => b.ID == editBookId);
            if (book != null)
            {
                BookNameBox.Text = book.BookName;
                DescriptionBox.Text = book.Description;
                TextBox.Text = book.Text;
                ImageBox.Text = book.Image;

                var bookGenres = Core.Context.BooksGenres
                    .Where(bg => bg.BookId == editBookId)
                    .Select(bg => bg.GenreId)
                    .ToList();

                foreach (var genre in genres)
                {
                    genre.IsSelected = bookGenres.Contains(genre.GenreId);
                }
                GenresListBox.Items.Refresh();
            }
        }

        /// <summary>
        /// Обработчик кнопки "Сохранить"
        /// </summary>
        /// <remarks>
        /// Выполняет валидацию всех полей:
        /// 1. Название - не должно быть пустым
        /// 2. Описание - не должно быть пустым
        /// 3. Текст книги - не должно быть пустым
        /// 4. Жанры - должен быть выбран хотя бы один
        /// 
        /// В зависимости от режима:
        /// - Редактирование: обновляет существующую книгу и её жанры
        /// - Добавление: создаёт новую книгу с привязкой к текущему автору
        /// 
        /// При обновлении жанров сначала удаляются старые связи (RemoveRange),
        /// затем добавляются новые (Add).
        /// 
        /// Путь к обложке: если не указан, используется "/images/default.jpg"
        /// </remarks>
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrWhiteSpace(BookNameBox.Text))
            {
                MessageBox.Show("Введите название книги");
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
            {
                MessageBox.Show("Введите описание книги");
                return;
            }

            if (string.IsNullOrWhiteSpace(TextBox.Text))
            {
                MessageBox.Show("Введите текст книги");
                return;
            }

            var selectedGenres = genres.Where(g => g.IsSelected).ToList();
            if (selectedGenres.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один жанр");
                return;
            }

            if (editBookId.HasValue)
            {

                var book = Core.Context.Books.FirstOrDefault(b => b.ID == editBookId);
                if (book != null)
                {
                    book.BookName = BookNameBox.Text.Trim();
                    book.Description = DescriptionBox.Text.Trim();
                    book.Text = TextBox.Text.Trim();
                    book.Image = string.IsNullOrWhiteSpace(ImageBox.Text) ? "/images/default.jpg" : ImageBox.Text.Trim();
                    Core.Context.SaveChanges();

                    var existingGenres = Core.Context.BooksGenres.Where(bg => bg.BookId == editBookId);
                    Core.Context.BooksGenres.RemoveRange(existingGenres);

                    foreach (var genre in selectedGenres)
                    {
                        Core.Context.BooksGenres.Add(new BooksGenres
                        {
                            BookId = editBookId.Value,
                            GenreId = genre.GenreId
                        });
                    }
                    Core.Context.SaveChanges();

                    MessageBox.Show("Книга обновлена!");
                }
            }
            else
            {

                var newBook = new Books
                {
                    BookName = BookNameBox.Text.Trim(),
                    Description = DescriptionBox.Text.Trim(),
                    Text = TextBox.Text.Trim(),
                    Image = string.IsNullOrWhiteSpace(ImageBox.Text) ? "/images/default.jpg" : ImageBox.Text.Trim(),
                    UserId = Session.CurrentUser.ID,
                    IsFrozen = false
                };
                Core.Context.Books.Add(newBook);
                Core.Context.SaveChanges();

                foreach (var genre in selectedGenres)
                {
                    Core.Context.BooksGenres.Add(new BooksGenres
                    {
                        BookId = newBook.ID,
                        GenreId = genre.GenreId
                    });
                }
                Core.Context.SaveChanges();

                MessageBox.Show("Книга добавлена!");
            }

            this.Close();
        }

        /// <summary>
        /// Обработчик кнопки "Отмена"
        /// </summary>
        /// <remarks>
        /// Закрывает окно без сохранения изменений.
        /// </remarks>
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    /// <summary>
    /// Модель для отображения жанра с чекбоксом в списке GenresListBox
    /// </summary>
    /// <remarks>
    /// Используется для множественного выбора жанров при добавлении/редактировании книги.
    /// Содержит:
    /// - GenreId: идентификатор жанра (для сохранения в BooksGenres)
    /// - GenreName: название жанра (для отображения пользователю)
    /// - IsSelected: флаг выбора (true - жанр отмечен)
    /// 
    /// Привязывается к ListBox с шаблоном CheckBox.
    /// </remarks>
    public class GenreCheckBox
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; }
        public bool IsSelected { get; set; }
    }
}