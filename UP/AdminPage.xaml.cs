using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UP
{
    /// <summary>
    /// Логика взаимодействия для AdminPage.xaml
    /// </summary>
    /// <remarks>
    /// Страница панели администратора. Доступна только пользователям с ролью "Администратор" (RoleId = 3).
    /// 
    /// Функциональные возможности:
    /// - Управление жалобами (принять/отклонить) на книги, отзывы и авторов
    /// - Управление заявками на разморозку (одобрить/отклонить) аккаунтов и книг
    /// - Управление заявками на роль автора (одобрить/отклонить)
    /// - Просмотр и разморозка замороженных пользователей
    /// - Просмотр и разморозка замороженных книг
    /// - Просмотр и разморозка замороженных отзывов
    /// - Управление пользователями (смена роли, смена пароля)
    /// </remarks>
    public partial class AdminPage : Page
    {
        /// <summary>
        /// Конструктор страницы администрирования.
        /// Инициализирует компоненты и загружает список жалоб (раздел по умолчанию).
        /// </summary>
        public AdminPage()
        {
            InitializeComponent();
            LoadComplaints();
        }

        /// <summary>
        /// Очищает правую область отображения контента.
        /// Вызывается перед загрузкой нового раздела.
        /// </summary>
        private void ClearContent()
        {
            ContentPanel.Children.Clear();
        }


        /// <summary>
        /// Обработчик кнопки "Жалобы" в боковом меню.
        /// Загружает и отображает список всех жалоб.
        /// </summary>
        private void ComplaintsBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadComplaints();
        }

        /// <summary>
        /// Загружает и отображает список всех жалоб из базы данных.
        /// Каждая жалоба отображается в виде карточки с информацией:
        /// - Кто пожаловался (UserName)
        /// - Тип цели (Книга/Отзыв/Автор)
        /// - Причина жалобы
        /// - Дата создания
        /// - Кнопки "Принять" и "Отклонить"
        /// 
        /// Принятие жалобы приводит к заморозке контента:
        /// - Книга: IsFrozen = true
        /// - Отзыв: IsFrozen = true
        /// - Пользователь: IsFrozen = true, сохраняется причина заморозки
        /// 
        /// Отклонение жалобы: запись просто удаляется, контент не меняется.
        /// </summary>
        private void LoadComplaints()
        {
            ClearContent();

            var complaints = (from c in Core.Context.Complaints
                              join u in Core.Context.Users on c.UserId equals u.ID
                              orderby c.CreatedDate descending
                              select new
                              {
                                  c.ID,
                                  c.Reason,
                                  c.CreatedDate,
                                  UserName = u.UserName,
                                  Target = c.BookId != null ? "Книга" : (c.ReviewId != null ? "Отзыв" : "Автор"),
                                  TargetId = c.BookId ?? c.ReviewId ?? c.TargetUserId
                              }).ToList();

            foreach (var complaint in complaints)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(15)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = $"Жалоба от {complaint.UserName}", FontWeight = FontWeights.Bold });
                stack.Children.Add(new TextBlock { Text = $"Тип: {complaint.Target}", Margin = new Thickness(0, 5, 0, 0) });
                stack.Children.Add(new TextBlock { Text = $"Причина: {complaint.Reason}", TextWrapping = TextWrapping.Wrap });
                stack.Children.Add(new TextBlock { Text = $"Дата: {complaint.CreatedDate:dd.MM.yyyy}", Foreground = Brushes.Gray, Margin = new Thickness(0, 5, 0, 0) });

                var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };

                var acceptBtn = new Button { Content = "Принять", Background = Brushes.Green, Foreground = Brushes.White, Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(10, 5, 10, 5) };
                acceptBtn.Click += (s, e) => AcceptComplaint(complaint.ID);
                buttonsPanel.Children.Add(acceptBtn);

                var rejectBtn = new Button { Content = "Отклонить", Background = Brushes.Red, Foreground = Brushes.White, Padding = new Thickness(10, 5, 10, 5) };
                rejectBtn.Click += (s, e) => RejectComplaint(complaint.ID);
                buttonsPanel.Children.Add(rejectBtn);

                stack.Children.Add(buttonsPanel);
                border.Child = stack;
                ContentPanel.Children.Add(border);
            }

            if (complaints.Count() == 0)
            {
                ContentPanel.Children.Add(new TextBlock { Text = "Нет жалоб", FontSize = 16, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });
            }
        }

        /// <summary>
        /// Принимает жалобу и замораживает соответствующий контент.
        /// </summary>
        /// <param name="complaintId">ID жалобы</param>
        private void AcceptComplaint(int complaintId)
        {
            var complaint = Core.Context.Complaints.FirstOrDefault(c => c.ID == complaintId);
            if (complaint != null)
            {
                if (complaint.BookId != null)
                {
                    var book = Core.Context.Books.FirstOrDefault(b => b.ID == complaint.BookId);
                    if (book != null) book.IsFrozen = true;
                }
                else if (complaint.ReviewId != null)
                {
                    var review = Core.Context.Reviews.FirstOrDefault(r => r.ID == complaint.ReviewId);
                    if (review != null) review.IsFrozen = true;
                }
                else if (complaint.TargetUserId != null)
                {
                    var user = Core.Context.Users.FirstOrDefault(u => u.ID == complaint.TargetUserId);
                    if (user != null)
                    {
                        user.IsFrozen = true;
                        user.FreezeReason = complaint.Reason;
                        user.FrozenAt = DateTime.Now;
                    }
                }
                Core.Context.Complaints.Remove(complaint);
                Core.Context.SaveChanges();
                MessageBox.Show("Жалоба принята, контент заморожен");
                LoadComplaints();
            }
        }

        /// <summary>
        /// Отклоняет жалобу (удаляет запись, контент не замораживается).
        /// </summary>
        /// <param name="complaintId">ID жалобы</param>
        private void RejectComplaint(int complaintId)
        {
            var complaint = Core.Context.Complaints.FirstOrDefault(c => c.ID == complaintId);
            if (complaint != null)
            {
                Core.Context.Complaints.Remove(complaint);
                Core.Context.SaveChanges();
                MessageBox.Show("Жалоба отклонена");
                LoadComplaints();
            }
        }


        /// <summary>
        /// Обработчик кнопки "Заявки на разморозку" в боковом меню.
        /// Загружает и отображает список активных заявок на разморозку.
        /// </summary>
        private void UnfreezeBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadUnfreezeRequests();
        }

        /// <summary>
        /// Загружает и отображает список заявок на разморозку со статусом "В ожидании".
        /// Каждая заявка содержит:
        /// - Имя пользователя, подавшего заявку
        /// - Тип (Аккаунт или Книга)
        /// - Причина разморозки
        /// - Дата подачи
        /// - Кнопки "Одобрить" и "Отклонить"
        /// 
        /// Одобрение заявки:
        /// - Для аккаунта: IsFrozen = false, очистка причины
        /// - Для книги: IsFrozen = false
        /// 
        /// Отклонение заявки: статус меняется на "Отклонена"
        /// </summary>
        private void LoadUnfreezeRequests()
        {
            ClearContent();

            var requests = (from ua in Core.Context.UnfreezeApplications
                            join u in Core.Context.Users on ua.UserId equals u.ID
                            where ua.Status == "В ожидании"
                            orderby ua.ApplicationDate descending
                            select new
                            {
                                ua.ID,
                                ua.Reason,
                                ua.ApplicationDate,
                                ua.TargetTypeId,
                                ua.TargetBookId,
                                UserName = u.UserName
                            }).ToList();

            foreach (var request in requests)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(15)
                };

                var stack = new StackPanel();
                string target = request.TargetTypeId == 1 ? "Аккаунт" : "Книга";
                stack.Children.Add(new TextBlock { Text = $"Заявка от {request.UserName} на разморозку {target}", FontWeight = FontWeights.Bold });
                stack.Children.Add(new TextBlock { Text = $"Причина: {request.Reason}", TextWrapping = TextWrapping.Wrap });
                stack.Children.Add(new TextBlock { Text = $"Дата: {request.ApplicationDate:dd.MM.yyyy}", Foreground = Brushes.Gray, Margin = new Thickness(0, 5, 0, 0) });

                var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };

                var acceptBtn = new Button { Content = "Одобрить", Background = Brushes.Green, Foreground = Brushes.White, Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(10, 5, 10, 5) };
                acceptBtn.Click += (s, e) => AcceptUnfreeze(request.ID, request.TargetTypeId, request.TargetBookId);
                buttonsPanel.Children.Add(acceptBtn);

                var rejectBtn = new Button { Content = "Отклонить", Background = Brushes.Red, Foreground = Brushes.White, Padding = new Thickness(10, 5, 10, 5) };
                rejectBtn.Click += (s, e) => RejectUnfreeze(request.ID);
                buttonsPanel.Children.Add(rejectBtn);

                stack.Children.Add(buttonsPanel);
                border.Child = stack;
                ContentPanel.Children.Add(border);
            }

            if (requests.Count() == 0)
            {
                ContentPanel.Children.Add(new TextBlock { Text = "Нет заявок на разморозку", FontSize = 16, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });
            }
        }

        /// <summary>
        /// Одобряет заявку на разморозку.
        /// </summary>
        /// <param name="requestId">ID заявки</param>
        /// <param name="targetTypeId">1 - Аккаунт, 2 - Книга</param>
        /// <param name="targetBookId">ID книги (только для targetTypeId = 2)</param>
        private void AcceptUnfreeze(int requestId, int targetTypeId, int? targetBookId)
        {
            var request = Core.Context.UnfreezeApplications.FirstOrDefault(r => r.ID == requestId);
            if (request != null)
            {
                if (targetTypeId == 1)
                {
                    var user = Core.Context.Users.FirstOrDefault(u => u.ID == request.UserId);
                    if (user != null)
                    {
                        user.IsFrozen = false;
                        user.FreezeReason = null;
                    }
                }
                else
                {
                    var book = Core.Context.Books.FirstOrDefault(b => b.ID == targetBookId);
                    if (book != null) book.IsFrozen = false;
                }
                request.Status = "Одобрена";
                Core.Context.SaveChanges();
                MessageBox.Show("Заявка одобрена");
                LoadUnfreezeRequests();
            }
        }

        /// <summary>
        /// Отклоняет заявку на разморозку (статус меняется на "Отклонена").
        /// </summary>
        /// <param name="requestId">ID заявки</param>
        private void RejectUnfreeze(int requestId)
        {
            var request = Core.Context.UnfreezeApplications.FirstOrDefault(r => r.ID == requestId);
            if (request != null)
            {
                request.Status = "Отклонена";
                Core.Context.SaveChanges();
                MessageBox.Show("Заявка отклонена");
                LoadUnfreezeRequests();
            }
        }


        /// <summary>
        /// Обработчик кнопки "Заявки на роль автора" в боковом меню.
        /// Загружает и отображает список заявок от читателей на получение роли автора.
        /// </summary>
        private void RoleRequestsBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadRoleRequests();
        }

        /// <summary>
        /// Загружает и отображает список заявок на роль автора со статусом "В ожидании".
        /// Каждая заявка содержит:
        /// - Имя пользователя
        /// - Email
        /// - Дата подачи
        /// - Кнопки "Одобрить" и "Отклонить"
        /// 
        /// Одобрение: пользователь получает RoleId = 2 (Автор)
        /// Отклонение: статус заявки меняется на "Отклонена"
        /// </summary>
        private void LoadRoleRequests()
        {
            ClearContent();

            var requests = (from ra in Core.Context.RoleApplications
                            join u in Core.Context.Users on ra.UserId equals u.ID
                            where ra.Status == "В ожидании"
                            orderby ra.ApplicationDate descending
                            select new
                            {
                                ra.ID,
                                ra.ApplicationDate,
                                UserName = u.UserName,
                                u.Email
                            }).ToList();

            foreach (var request in requests)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(15)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = $"Заявка от {request.UserName}", FontWeight = FontWeights.Bold });
                stack.Children.Add(new TextBlock { Text = $"Email: {request.Email}" });
                stack.Children.Add(new TextBlock { Text = $"Дата: {request.ApplicationDate:dd.MM.yyyy}", Foreground = Brushes.Gray, Margin = new Thickness(0, 5, 0, 0) });

                var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };

                var acceptBtn = new Button { Content = "Одобрить", Background = Brushes.Green, Foreground = Brushes.White, Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(10, 5, 10, 5) };
                acceptBtn.Click += (s, e) => AcceptRoleRequest(request.ID);
                buttonsPanel.Children.Add(acceptBtn);

                var rejectBtn = new Button { Content = "Отклонить", Background = Brushes.Red, Foreground = Brushes.White, Padding = new Thickness(10, 5, 10, 5) };
                rejectBtn.Click += (s, e) => RejectRoleRequest(request.ID);
                buttonsPanel.Children.Add(rejectBtn);

                stack.Children.Add(buttonsPanel);
                border.Child = stack;
                ContentPanel.Children.Add(border);
            }

            if (requests.Count() == 0)
            {
                ContentPanel.Children.Add(new TextBlock { Text = "Нет заявок на роль автора", FontSize = 16, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });
            }
        }

        /// <summary>
        /// Одобряет заявку на роль автора.
        /// Меняет роль пользователя на "Автор" (RoleId = 2).
        /// </summary>
        /// <param name="requestId">ID заявки</param>
        private void AcceptRoleRequest(int requestId)
        {
            var request = Core.Context.RoleApplications.FirstOrDefault(r => r.ID == requestId);
            if (request != null)
            {
                var user = Core.Context.Users.FirstOrDefault(u => u.ID == request.UserId);
                if (user != null) user.RoleId = 2;
                request.Status = "Одобрена";
                Core.Context.SaveChanges();
                MessageBox.Show("Заявка одобрена, пользователь стал автором");
                LoadRoleRequests();
            }
        }

        /// <summary>
        /// Отклоняет заявку на роль автора.
        /// Статус заявки меняется на "Отклонена".
        /// </summary>
        /// <param name="requestId">ID заявки</param>
        private void RejectRoleRequest(int requestId)
        {
            var request = Core.Context.RoleApplications.FirstOrDefault(r => r.ID == requestId);
            if (request != null)
            {
                request.Status = "Отклонена";
                Core.Context.SaveChanges();
                MessageBox.Show("Заявка отклонена");
                LoadRoleRequests();
            }
        }


        /// <summary>
        /// Обработчик кнопки "Замороженные пользователи" в боковом меню.
        /// Загружает и отображает список всех замороженных пользователей.
        /// </summary>
        private void FrozenUsersBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadFrozenUsers();
        }

        /// <summary>
        /// Загружает и отображает список замороженных пользователей.
        /// Для каждого пользователя показывает:
        /// - Имя и логин
        /// - Причину заморозки
        /// - Дату заморозки
        /// - Кнопку "Разморозить"
        /// </summary>
        private void LoadFrozenUsers()
        {
            ClearContent();

            var users = Core.Context.Users.Where(u => u.IsFrozen == true).ToList();

            foreach (var user in users)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(15)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = $"🚫 {user.UserName} (логин: {user.Login})", FontWeight = FontWeights.Bold });
                stack.Children.Add(new TextBlock { Text = $"Причина: {user.FreezeReason ?? "Не указана"}", TextWrapping = TextWrapping.Wrap });
                stack.Children.Add(new TextBlock { Text = $"Дата заморозки: {user.FrozenAt:dd.MM.yyyy}", Foreground = Brushes.Gray, Margin = new Thickness(0, 5, 0, 0) });

                var unblockBtn = new Button { Content = "Разморозить", Background = Brushes.Green, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(10, 5, 10, 5) };
                unblockBtn.Click += (s, e) => UnblockUser(user.ID);
                stack.Children.Add(unblockBtn);

                border.Child = stack;
                ContentPanel.Children.Add(border);
            }

            if (users.Count() == 0)
            {
                ContentPanel.Children.Add(new TextBlock { Text = "Нет замороженных пользователей", FontSize = 16, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });
            }
        }

        /// <summary>
        /// Размораживает пользователя (IsFrozen = false, очищает причину заморозки).
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        private void UnblockUser(int userId)
        {
            var user = Core.Context.Users.FirstOrDefault(u => u.ID == userId);
            if (user != null)
            {
                user.IsFrozen = false;
                user.FreezeReason = null;
                Core.Context.SaveChanges();
                MessageBox.Show("Пользователь разморожен");
                LoadFrozenUsers();
            }
        }


        /// <summary>
        /// Обработчик кнопки "Замороженные книги" в боковом меню.
        /// Загружает и отображает список всех замороженных книг.
        /// </summary>
        private void FrozenBooksBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadFrozenBooks();
        }

        /// <summary>
        /// Загружает и отображает список замороженных книг.
        /// Для каждой книги показывает:
        /// - Название книги
        /// - Автора
        /// - Кнопку "Разморозить"
        /// </summary>
        private void LoadFrozenBooks()
        {
            ClearContent();

            var books = (from b in Core.Context.Books
                         join u in Core.Context.Users on b.UserId equals u.ID
                         where b.IsFrozen == true
                         select new
                         {
                             b.ID,
                             b.BookName,
                             AuthorName = u.UserName
                         }).ToList();

            foreach (var book in books)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(15)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = $"{book.BookName}", FontWeight = FontWeights.Bold });
                stack.Children.Add(new TextBlock { Text = $"Автор: {book.AuthorName}" });

                var unblockBtn = new Button { Content = "Разморозить", Background = Brushes.Green, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(10, 5, 10, 5) };
                unblockBtn.Click += (s, e) => UnblockBook(book.ID);
                stack.Children.Add(unblockBtn);

                border.Child = stack;
                ContentPanel.Children.Add(border);
            }

            if (books.Count() == 0)
            {
                ContentPanel.Children.Add(new TextBlock { Text = "Нет замороженных книг", FontSize = 16, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });
            }
        }

        /// <summary>
        /// Размораживает книгу (IsFrozen = false).
        /// </summary>
        /// <param name="bookId">ID книги</param>
        private void UnblockBook(int bookId)
        {
            var book = Core.Context.Books.FirstOrDefault(b => b.ID == bookId);
            if (book != null)
            {
                book.IsFrozen = false;
                Core.Context.SaveChanges();
                MessageBox.Show("Книга разморожена");
                LoadFrozenBooks();
            }
        }


        /// <summary>
        /// Обработчик кнопки "Замороженные отзывы" в боковом меню.
        /// Загружает и отображает список всех замороженных отзывов.
        /// </summary>
        private void FrozenReviewsBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadFrozenReviews();
        }

        /// <summary>
        /// Загружает и отображает список замороженных отзывов.
        /// Для каждого отзыва показывает:
        /// - Имя пользователя, оставившего отзыв
        /// - Название книги
        /// - Оценку (⭐)
        /// - Текст отзыва
        /// - Кнопку "Разморозить"
        /// </summary>
        private void LoadFrozenReviews()
        {
            ClearContent();

            var reviews = (from r in Core.Context.Reviews
                           join b in Core.Context.Books on r.BookId equals b.ID
                           join u in Core.Context.Users on r.UserId equals u.ID
                           where r.IsFrozen == true
                           select new
                           {
                               r.ID,
                               BookName = b.BookName,
                               UserName = u.UserName,
                               r.Text,
                               r.Rating
                           }).ToList();

            foreach (var review in reviews)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(15)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = $"💬 Отзыв от {review.UserName} на книгу '{review.BookName}'", FontWeight = FontWeights.Bold });
                stack.Children.Add(new TextBlock { Text = $"Оценка: ⭐ {review.Rating}/10" });
                stack.Children.Add(new TextBlock { Text = $"Текст: {review.Text}", TextWrapping = TextWrapping.Wrap });

                var unblockBtn = new Button { Content = "Разморозить", Background = Brushes.Green, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(10, 5, 10, 5) };
                unblockBtn.Click += (s, e) => UnblockReview(review.ID);
                stack.Children.Add(unblockBtn);

                border.Child = stack;
                ContentPanel.Children.Add(border);
            }

            if (reviews.Count() == 0)
            {
                ContentPanel.Children.Add(new TextBlock { Text = "Нет замороженных отзывов", FontSize = 16, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });
            }
        }

        /// <summary>
        /// Размораживает отзыв (IsFrozen = false).
        /// </summary>
        /// <param name="reviewId">ID отзыва</param>
        private void UnblockReview(int reviewId)
        {
            var review = Core.Context.Reviews.FirstOrDefault(r => r.ID == reviewId);
            if (review != null)
            {
                review.IsFrozen = false;
                Core.Context.SaveChanges();
                MessageBox.Show("Отзыв разморожен");
                LoadFrozenReviews();
            }
        }


        /// <summary>
        /// Обработчик кнопки "Все пользователи" в боковом меню.
        /// Загружает и отображает список всех пользователей системы.
        /// </summary>
        private void UsersBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadAllUsers();
        }

        /// <summary>
        /// Загружает и отображает список всех пользователей системы.
        /// Для каждого пользователя показывает:
        /// - Имя и логин
        /// - Email
        /// - Текущую роль
        /// - ComboBox для выбора новой роли
        /// - Кнопку "Сменить роль"
        /// - Поле для ввода нового пароля
        /// - Кнопку "Сменить пароль"
        /// 
        /// При смене роли:
        /// - Если роль меняется с "Автор" на "Читатель", удаляется одобренная заявка на роль автора
        /// </summary>
        private void LoadAllUsers()
        {
            ClearContent();

            var users = (from u in Core.Context.Users
                         join r in Core.Context.Roles on u.RoleId equals r.ID
                         select new
                         {
                             u.ID,
                             u.UserName,
                             u.Login,
                             u.Email,
                             RoleName = r.RoleName
                         }).ToList();

            foreach (var user in users)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(15)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = $"👤 {user.UserName} (логин: {user.Login})", FontWeight = FontWeights.Bold });
                stack.Children.Add(new TextBlock { Text = $"Email: {user.Email}" });
                stack.Children.Add(new TextBlock { Text = $"Текущая роль: {user.RoleName}", Margin = new Thickness(0, 5, 0, 0) });

                var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };

                var roleCombo = new ComboBox { Width = 120, Margin = new Thickness(0, 0, 10, 0) };
                var roles = Core.Context.Roles.ToList();
                foreach (var role in roles)
                {
                    roleCombo.Items.Add(new ComboBoxItem { Content = role.RoleName, Tag = role.ID });
                    if (role.RoleName == user.RoleName)
                        roleCombo.SelectedItem = roleCombo.Items[roleCombo.Items.Count - 1];
                }
                buttonsPanel.Children.Add(roleCombo);

                var changeRoleBtn = new Button { Content = "Сменить роль", Background = Brushes.Orange, Foreground = Brushes.White, Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(10, 5, 10, 5) };
                changeRoleBtn.Click += (s, e) => ChangeRole(user.ID, (roleCombo.SelectedItem as ComboBoxItem)?.Tag as int?);
                buttonsPanel.Children.Add(changeRoleBtn);

                var newPassBox = new PasswordBox { Width = 120, Margin = new Thickness(0, 0, 10, 0) };
                buttonsPanel.Children.Add(newPassBox);

                var changePassBtn = new Button { Content = "Сменить пароль", Background = Brushes.Blue, Foreground = Brushes.White, Padding = new Thickness(10, 5, 10, 5) };
                changePassBtn.Click += (s, e) => ChangePassword(user.ID, newPassBox.Password);
                buttonsPanel.Children.Add(changePassBtn);

                stack.Children.Add(buttonsPanel);
                border.Child = stack;
                ContentPanel.Children.Add(border);
            }
        }

        /// <summary>
        /// Изменяет роль пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="newRoleId">ID новой роли (1-Читатель, 2-Автор, 3-Администратор)</param>
        private void ChangeRole(int userId, int? newRoleId)
        {
            if (newRoleId == null) return;

            var user = Core.Context.Users.FirstOrDefault(u => u.ID == userId);
            if (user != null)
            {
                user.RoleId = newRoleId;

                if (newRoleId == 1)
                {
                    var application = Core.Context.RoleApplications
                        .FirstOrDefault(r => r.UserId == userId && r.Status == "Одобрена");
                    if (application != null)
                    {
                        Core.Context.RoleApplications.Remove(application);
                    }
                }

                Core.Context.SaveChanges();
                MessageBox.Show("Роль изменена");
                LoadAllUsers();
            }
        }

        /// <summary>
        /// Изменяет пароль пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="newPassword">Новый пароль</param>
        private void ChangePassword(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Введите новый пароль");
                return;
            }

            var user = Core.Context.Users.FirstOrDefault(u => u.ID == userId);
            if (user != null)
            {
                user.Password = newPassword;
                Core.Context.SaveChanges();
                MessageBox.Show("Пароль изменён");
                LoadAllUsers();
            }
        }
    }
}