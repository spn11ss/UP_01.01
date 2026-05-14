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
    /// Логика взаимодействия для ProfilePage.xaml
    /// </summary>
    /// <remarks>
    /// Страница профиля пользователя. Отображает личную информацию и предоставляет функции управления аккаунтом.
    /// 
    /// Функциональные возможности:
    /// - Просмотр личной информации (имя, логин, email, роль)
    /// - Просмотр всех отзывов, оставленных пользователем
    /// - Подача заявки на роль автора (для читателей)
    /// - Предупреждение о заморозке аккаунта с причиной
    /// - Оспаривание заморозки аккаунта (подача заявки на разморозку)
    /// 
    /// Доступна всем авторизованным пользователям независимо от роли.
    /// </remarks>
    public partial class ProfilePage : Page
    {
        /// <summary>
        /// Конструктор страницы профиля
        /// </summary>
        /// <remarks>
        /// Выполняет последовательную инициализацию:
        /// 1. InitializeComponent() - загрузка XAML разметки
        /// 2. LoadUserInfo() - загрузка и отображение данных пользователя
        /// 3. LoadUserReviews() - загрузка отзывов пользователя
        /// 4. CheckAuthorRequest() - проверка статуса заявки на роль автора
        /// 5. CheckUnfreezeRequest() - проверка статуса заявки на разморозку
        /// </remarks>
        public ProfilePage()
        {
            InitializeComponent();
            LoadUserInfo();
            LoadUserReviews();
            CheckAuthorRequest();
            CheckUnfreezeRequest();
        }

        /// <summary>
        /// Загружает и отображает информацию о текущем пользователе
        /// </summary>
        /// <remarks>
        /// Отображаемые данные:
        /// - UserName (имя пользователя)
        /// - Login (логин)
        /// - Email (электронная почта)
        /// - RoleName (название роли из таблицы Roles)
        /// 
        /// Дополнительно:
        /// - Если аккаунт заморожен (IsFrozen == true) - показывает предупреждение с причиной
        /// - Если пользователь - читатель (RoleId == 1) - показывает кнопку подачи заявки на автора
        /// 
        /// Данные берутся из Session.CurrentUser (устанавливается при входе в систему).
        /// </remarks>
        private void LoadUserInfo()
        {
            if (!Session.IsAuthenticated)
            {
                return;
            }

            var user = Session.CurrentUser;

            UserNameText.Text = user.UserName;
            LoginText.Text = user.Login;
            EmailText.Text = user.Email;

            var role = Core.Context.Roles.FirstOrDefault(r => r.ID == user.RoleId);
            RoleText.Text = role?.RoleName ?? "Не указана";

            if (user.IsFrozen == true)
            {
                FrozenWarning.Visibility = Visibility.Visible;
                FreezeReasonText.Text = $"Причина: {user.FreezeReason ?? "Не указана"}";
            }

            if (user.RoleId == 1)
            {
                AuthorRequestPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Загружает и отображает отзывы текущего пользователя
        /// </summary>
        /// <remarks>
        /// Формирует запрос к базе данных:
        /// 1. Выбирает отзывы текущего пользователя (UserId == Session.CurrentUser.ID)
        /// 2. Исключает замороженные отзывы (IsFrozen == false)
        /// 3. Присоединяет название книги из таблицы Books
        /// 4. Сортирует по дате отзыва (сначала новые)
        /// 
        /// Результат:
        /// - Если есть отзывы - отображает их в виде списка
        /// - Если отзывов нет - показывает сообщение "У вас пока нет отзывов"
        /// </remarks>
        private void LoadUserReviews()
        {
            if (!Session.IsAuthenticated)
            {
                return;
            }

            var reviews = (from r in Core.Context.Reviews
                           join b in Core.Context.Books on r.BookId equals b.ID
                           where r.UserId == Session.CurrentUser.ID && r.IsFrozen == false
                           orderby r.DateReview descending
                           select new UserReviewViewModel
                           {
                               BookName = b.BookName,
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
        /// Проверяет статус заявки пользователя на роль автора
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Ищет заявку текущего пользователя в таблице RoleApplications
        /// 2. Если заявка одобрена, но пользователь не автор - удаляет устаревшую заявку (несоответствие)
        /// 3. В зависимости от статуса заявки отображает соответствующую панель:
        ///    - Нет заявки: показывает кнопку "Подать заявку"
        ///    - "В ожидании": оранжевая панель с текстом "Заявка рассматривается"
        ///    - "Одобрена": зелёная панель с текстом "Заявка одобрена! Теперь вы автор"
        ///    - "Отклонена": красная панель с текстом "Заявка отклонена"
        /// 
        /// Важно: если роль пользователя уже автор (RoleId == 2), заявка не отображается.
        /// </remarks>
        private void CheckAuthorRequest()
        {
            if (!Session.IsAuthenticated) return;

            var existingRequest = Core.Context.RoleApplications
                .FirstOrDefault(r => r.UserId == Session.CurrentUser.ID);

            if (existingRequest != null && existingRequest.Status == "Одобрена" && Session.CurrentUser.RoleId != 2)
            {
                Core.Context.RoleApplications.Remove(existingRequest);
                Core.Context.SaveChanges();
                existingRequest = null;
            }

            if (existingRequest == null)
            {
                AuthorRequestPanel.Visibility = Visibility.Visible;
                RequestStatusPanel.Visibility = Visibility.Collapsed;
                return;
            }

            if (existingRequest.Status == "В ожидании")
            {
                RequestStatusText.Text = "Заявка на роль автора рассматривается";
                RequestStatusPanel.Background = Brushes.Orange;
                AuthorRequestPanel.Visibility = Visibility.Collapsed;
                RequestStatusPanel.Visibility = Visibility.Visible;
            }
            else if (existingRequest.Status == "Одобрена")
            {
                RequestStatusText.Text = "Заявка одобрена! Теперь вы автор";
                RequestStatusPanel.Background = Brushes.Green;
                AuthorRequestPanel.Visibility = Visibility.Collapsed;
                RequestStatusPanel.Visibility = Visibility.Visible;
            }
            else if (existingRequest.Status == "Отклонена")
            {
                RequestStatusText.Text = "Заявка отклонена";
                RequestStatusPanel.Background = Brushes.Red;
                AuthorRequestPanel.Visibility = Visibility.Collapsed;
                RequestStatusPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Проверяет статус активной заявки на разморозку аккаунта
        /// </summary>
        /// <remarks>
        /// Если у пользователя есть заявка на разморозку со статусом "В ожидании":
        /// - Кнопка "Оспорить заморозку" становится неактивной (IsEnabled = false)
        /// - Текст кнопки меняется на "Заявка уже подана"
        /// - Фон кнопки становится серым (Brushes.Gray)
        /// 
        /// Это предотвращает создание множественных заявок одним пользователем.
        /// </remarks>
        private void CheckUnfreezeRequest()
        {
            if (!Session.IsAuthenticated) return;

            var existingRequest = Core.Context.UnfreezeApplications
                .FirstOrDefault(u => u.UserId == Session.CurrentUser.ID && u.Status == "В ожидании");

            if (existingRequest != null)
            {
                AppealBtn.IsEnabled = false;
                AppealBtn.Content = "Заявка уже подана";
                AppealBtn.Background = Brushes.Gray;
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Подать заявку" на роль автора
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Запрашивает подтверждение пользователя
        /// 2. Создаёт новую заявку в таблице RoleApplications со статусом "В ожидании"
        /// 3. Сохраняет изменения в базе данных
        /// 4. Показывает сообщение об успехе
        /// 5. Обновляет интерфейс (CheckAuthorRequest)
        /// 
        /// Заявка будет рассмотрена администратором на странице AdminPage.
        /// </remarks>
        private void RequestAuthorBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Подать заявку на роль автора?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var application = new RoleApplications
                {
                    UserId = Session.CurrentUser.ID,
                    ApplicationDate = DateTime.Now,
                    Status = "В ожидании"
                };

                Core.Context.RoleApplications.Add(application);
                Core.Context.SaveChanges();

                MessageBox.Show("Заявка подана!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CheckAuthorRequest();
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Оспорить заморозку"
        /// </summary>
        /// <remarks>
        /// Алгоритм:
        /// 1. Проверяет, нет ли уже активной заявки (предотвращает дублирование)
        /// 2. Открывает окно AppealWindow для ввода причины
        /// 3. Если причина указана - создаёт новую заявку в UnfreezeApplications
        /// 4. Сохраняет в базе данных со статусом "В ожидании"
        /// 5. Выводит сообщение об успехе
        /// 6. Деактивирует кнопку (CheckUnfreezeRequest)
        /// 
        /// Заявка будет рассмотрена администратором на странице AdminPage.
        /// </remarks>
        private void AppealBtn_Click(object sender, RoutedEventArgs e)
        {
            var existing = Core.Context.UnfreezeApplications
                .FirstOrDefault(u => u.UserId == Session.CurrentUser.ID && u.Status == "В ожидании");

            if (existing != null)
            {
                MessageBox.Show("Вы уже подали заявку на разморозку. Ожидайте решения администратора.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AppealWindow appealWindow = new AppealWindow("Оспаривание заморозки аккаунта");
            appealWindow.ShowDialog();

            if (appealWindow.IsSubmitted)
            {
                var appeal = new UnfreezeApplications
                {
                    UserId = Session.CurrentUser.ID,
                    TargetTypeId = 1,
                    TargetBookId = null,
                    Reason = appealWindow.Reason,
                    ApplicationDate = DateTime.Now,
                    Status = "В ожидании"
                };

                Core.Context.UnfreezeApplications.Add(appeal);
                Core.Context.SaveChanges();

                MessageBox.Show("Заявка на разморозку подана!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CheckUnfreezeRequest();
            }
        }
    }

    /// <summary>
    /// Модель представления для отзыва пользователя
    /// </summary>
    /// <remarks>
    /// Используется для отображения отзывов на странице профиля.
    /// Содержит:
    /// - BookName: название книги, на которую оставлен отзыв
    /// - Text: текст отзыва
    /// - Rating: оценка (от 1 до 10)
    /// - DateReview: дата написания отзыва
    /// 
    /// Данные формируются через JOIN таблиц Reviews и Books.
    /// </remarks>
    public class UserReviewViewModel
    {
        public string BookName { get; set; }
        public string Text { get; set; }
        public int Rating { get; set; }
        public DateTime DateReview { get; set; }
    }
}