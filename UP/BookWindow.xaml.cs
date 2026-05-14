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
    /// Логика взаимодействия для BookWindow.xaml
    /// </summary>
    /// <remarks>
    /// Страница детального просмотра книги. Отображает полную информацию о книге:
    /// - Название, автор, описание, жанры, рейтинг
    /// - Обложка книги
    /// - Полный текст книги для чтения
    /// - Отзывы пользователей с возможностью добавления нового отзыва
    /// 
    /// Функциональные возможности:
    /// - Добавление книги в пользовательские списки (В планах/Читаю/Прочитано/Заброшено)
    /// - Подача жалобы на книгу
    /// - Подача жалобы на автора
    /// - Подача жалобы на отзыв (доступно всем авторизованным)
    /// - Заморозка книги (доступно только администратору)
    /// - Заморозка отзыва (доступно только администратору)
    /// - Добавление нового отзыва с оценкой (от 1 до 10)
    /// 
    /// Доступна всем авторизованным и неавторизованным пользователям (чтение),
    /// но для взаимодействия требуется авторизация.
    /// </remarks>
    public partial class BookWindow : Page
    {
        /// <summary>
        /// ID текущей отображаемой книги
        /// </summary>
        private int bookId;

        /// <summary>
        /// ViewModel для привязки данных книги к интерфейсу
        /// </summary>
        private BookViewModel viewModel;

        /// <summary>
        /// Конструктор страницы книги
        /// </summary>
        /// <param name="bookId">ID книги для отображения</param>
        /// <remarks>
        /// Выполняет:
        /// 1. InitializeComponent() - загрузка XAML разметки
        /// 2. Сохраняет ID книги для дальнейших операций
        /// 3. Загружает данные книги (LoadBookData)
        /// 4. Загружает отзывы (LoadReviews)
        /// 5. Если пользователь администратор - показывает кнопку "Заморозить книгу"
        /// </remarks>
        public BookWindow(int bookId)
        {
            InitializeComponent();
            this.bookId = bookId;
            LoadBookData();
            LoadReviews();

            if (Session.IsAdmin)
            {
                FreezeBookBtn.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Загружает данные книги из базы данных и устанавливает DataContext
        /// </summary>
        /// <remarks>
        /// Формирует запрос к базе данных:
        /// 1. Получает книгу по ID из таблицы Books
        /// 2. Получает имя автора из таблицы Users
        /// 3. Получает список жанров через BooksGenres и Genres
        /// 4. Вычисляет средний рейтинг по незамороженным отзывам
        /// 
        /// Все данные сохраняются в BookViewModel и устанавливаются как DataContext
        /// для привязки к элементам XAML.
        /// 
        /// Если книга не найдена - выводится сообщение об ошибке.
        /// </remarks>
        private void LoadBookData()
        {
            var book = Core.Context.Books.FirstOrDefault(b => b.ID == bookId);
            if (book == null)
            {
                MessageBox.Show("Книга не найдена");
                return;
            }

            viewModel = new BookViewModel();
            viewModel.ID = book.ID;
            viewModel.BookName = book.BookName;
            viewModel.Description = book.Description;
            viewModel.Text = book.Text;
            viewModel.Image = book.Image;

            var author = Core.Context.Users.FirstOrDefault(u => u.ID == book.UserId);
            viewModel.AuthorName = $"Автор: {author?.UserName ?? "Неизвестен"}";

            var genres = from bg in Core.Context.BooksGenres
                         join g in Core.Context.Genres on bg.GenreId equals g.ID
                         where bg.BookId == bookId
                         select g.GenreName;
            viewModel.Genres = string.Join(", ", genres.ToList());

            var reviews = Core.Context.Reviews.Where(r => r.BookId == bookId && r.IsFrozen == false);
            double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            viewModel.Rating = avgRating > 0 ? $"⭐ {avgRating:F1}/10 (Отзывы: {reviews.Count()})" : "Нет оценок";

            DataContext = viewModel;
        }

        /// <summary>
        /// Загружает и отображает отзывы на книгу
        /// </summary>
        /// <remarks>
        /// Формирует запрос к базе данных:
        /// 1. Выбирает отзывы для текущей книги (BookId == bookId)
        /// 2. Исключает замороженные отзывы (IsFrozen == false)
        /// 3. Присоединяет имя пользователя из таблицы Users
        /// 4. Сортирует отзывы по дате (сначала новые)
        /// 
        /// Результат:
        /// - Если есть отзывы - отображает их в списке
        /// - Если отзывов нет - показывает сообщение "Пока нет отзывов"
        /// </remarks>
        private void LoadReviews()
        {
            var reviews = (from r in Core.Context.Reviews
                           join u in Core.Context.Users on r.UserId equals u.ID
                           where r.BookId == bookId && r.IsFrozen == false
                           orderby r.DateReview descending
                           select new ReviewViewModel
                           {
                               ID = r.ID,
                               UserName = u.UserName,
                               Text = r.Text,
                               Rating = r.Rating,
                               DateReview = r.DateReview
                           }).ToList();

            if (reviews.Count == 0)
            {
                NoReviewsText.Visibility = Visibility.Visible;
                ReviewsItemsControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                ReviewsItemsControl.ItemsSource = reviews;
            }
        }

        /// <summary>
        /// Обработчик кнопки "В список" - добавление книги в пользовательский список
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Проверяет авторизацию пользователя
        /// 2. Проверяет, не добавлена ли уже книга в списки
        /// 3. Открывает окно SelectStatusWindow для выбора статуса
        /// 
        /// Доступно только авторизованным пользователям.
        /// </remarks>
        private void AddToListBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuthenticated)
            {
                MessageBox.Show("Войдите в систему");
                return;
            }

            var existing = Core.Context.ReadingLists
                .FirstOrDefault(rl => rl.UserId == Session.CurrentUser.ID && rl.BookId == bookId);

            if (existing != null)
            {
                MessageBox.Show("Книга уже в вашем списке");
                return;
            }

            SelectStatusWindow statusWindow = new SelectStatusWindow(bookId);
            statusWindow.ShowDialog();
        }

        /// <summary>
        /// Обработчик кнопки "Жалоба на книгу"
        /// </summary>
        /// <remarks>
        /// Открывает окно ComplaintWindow для ввода причины жалобы.
        /// После подтверждения создаёт запись в таблице Complaints с BookId.
        /// Доступно только авторизованным пользователям.
        /// </remarks>
        private void ComplainBookBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuthenticated)
            {
                MessageBox.Show("Войдите в систему");
                return;
            }

            ComplaintWindow complaintWindow = new ComplaintWindow("Жалоба на книгу");
            complaintWindow.ShowDialog();

            if (complaintWindow.IsSubmitted)
            {
                var complaint = new Complaints
                {
                    UserId = Session.CurrentUser.ID,
                    BookId = bookId,
                    ReviewId = null,
                    TargetUserId = null,
                    Reason = complaintWindow.Reason,
                    CreatedDate = DateTime.Now
                };
                Core.Context.Complaints.Add(complaint);
                Core.Context.SaveChanges();
                MessageBox.Show("Жалоба отправлена");
            }
        }

        /// <summary>
        /// Обработчик кнопки "Жалоба на автора"
        /// </summary>
        /// <remarks>
        /// Открывает окно ComplaintWindow для ввода причины жалобы.
        /// После подтверждения создаёт запись в таблице Complaints с TargetUserId.
        /// Доступно только авторизованным пользователям.
        /// </remarks>
        private void ComplainAuthorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuthenticated)
            {
                MessageBox.Show("Войдите в систему");
                return;
            }

            ComplaintWindow complaintWindow = new ComplaintWindow("Жалоба на автора");
            complaintWindow.ShowDialog();

            if (complaintWindow.IsSubmitted)
            {
                var complaint = new Complaints
                {
                    UserId = Session.CurrentUser.ID,
                    BookId = null,
                    ReviewId = null,
                    TargetUserId = viewModel.ID,
                    Reason = complaintWindow.Reason,
                    CreatedDate = DateTime.Now
                };
                Core.Context.Complaints.Add(complaint);
                Core.Context.SaveChanges();
                MessageBox.Show("Жалоба отправлена");
            }
        }

        /// <summary>
        /// Обработчик кнопки "Заморозить книгу" (только для администратора)
        /// </summary>
        /// <remarks>
        /// Запрашивает подтверждение и устанавливает IsFrozen = true для книги.
        /// После заморозки книга скрывается из каталога.
        /// Кнопка становится неактивной.
        /// Доступно только пользователям с ролью "Администратор" (RoleId == 3).
        /// </remarks>
        private void FreezeBookBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Заморозить книгу?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var book = Core.Context.Books.FirstOrDefault(b => b.ID == bookId);
                if (book != null)
                {
                    book.IsFrozen = true;
                    Core.Context.SaveChanges();
                    MessageBox.Show("Книга заморожена");
                    FreezeBookBtn.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки "Оставить отзыв"
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Проверяет авторизацию пользователя
        /// 2. Проверяет, что текст отзыва не пустой
        /// 3. Получает выбранную оценку из RatingBox (1-10)
        /// 4. Создаёт запись в таблице Reviews
        /// 5. Обновляет список отзывов и рейтинг книги
        /// 
        /// Доступно только авторизованным пользователям.
        /// </remarks>
        private void SubmitReviewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuthenticated)
            {
                MessageBox.Show("Войдите в систему");
                return;
            }

            if (string.IsNullOrWhiteSpace(ReviewText.Text))
            {
                MessageBox.Show("Введите текст отзыва");
                return;
            }

            var selectedRating = RatingBox.SelectedItem as ComboBoxItem;
            int rating = int.Parse(selectedRating.Content.ToString().Replace("⭐ ", ""));

            var review = new Reviews
            {
                UserId = Session.CurrentUser.ID,
                BookId = bookId,
                Text = ReviewText.Text.Trim(),
                Rating = rating,
                DateReview = DateTime.Now,
                IsFrozen = false
            };

            Core.Context.Reviews.Add(review);
            Core.Context.SaveChanges();

            MessageBox.Show("Отзыв добавлен");

            ReviewText.Text = "";
            RatingBox.SelectedIndex = 7;
            LoadReviews();
            LoadBookData();
        }

        /// <summary>
        /// Обработчик кнопки жалобы на отзыв
        /// </summary>
        /// <remarks>
        /// Открывает окно ComplaintWindow для ввода причины жалобы.
        /// После подтверждения создаёт запись в таблице Complaints с ReviewId.
        /// Доступно только авторизованным пользователям.
        /// </remarks>
        private void ComplainReviewBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int reviewId = (int)btn.Tag;

            ComplaintWindow complaintWindow = new ComplaintWindow("Жалоба на отзыв");
            complaintWindow.ShowDialog();

            if (complaintWindow.IsSubmitted)
            {
                var complaint = new Complaints
                {
                    UserId = Session.CurrentUser.ID,
                    BookId = null,
                    ReviewId = reviewId,
                    TargetUserId = null,
                    Reason = complaintWindow.Reason,
                    CreatedDate = DateTime.Now
                };
                Core.Context.Complaints.Add(complaint);
                Core.Context.SaveChanges();
                MessageBox.Show("Жалоба отправлена");
            }
        }

        /// <summary>
        /// Обработчик кнопки заморозки отзыва (только для администратора)
        /// </summary>
        /// <remarks>
        /// Запрашивает подтверждение и устанавливает IsFrozen = true для отзыва.
        /// После заморозки отзыв скрывается из списка и не учитывается в рейтинге.
        /// Доступно только пользователям с ролью "Администратор" (RoleId == 3).
        /// </remarks>
        private void FreezeReviewBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int reviewId = (int)btn.Tag;

            var result = MessageBox.Show("Заморозить отзыв?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var review = Core.Context.Reviews.FirstOrDefault(r => r.ID == reviewId);
                if (review != null)
                {
                    review.IsFrozen = true;
                    Core.Context.SaveChanges();
                    MessageBox.Show("Отзыв заморожен");
                    LoadReviews();
                    LoadBookData();
                }
            }
        }
    }

    /// <summary>
    /// Модель представления для отзыва
    /// </summary>
    /// <remarks>
    /// Используется для отображения отзывов на странице книги.
    /// Содержит:
    /// - ID: идентификатор отзыва (для операций заморозки/жалобы)
    /// - UserName: имя пользователя, оставившего отзыв
    /// - Text: текст отзыва
    /// - Rating: оценка (от 1 до 10)
    /// - DateReview: дата написания отзыва
    /// </remarks>
    public class ReviewViewModel
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
        public int Rating { get; set; }
        public DateTime DateReview { get; set; }
    }
}