using Core.Convertors;
using Core.DTO;
using Core.Genarate;
using Core.Security;
using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core.Services
{
    public class Userservice : IUserService
    {
        private AsanwebContext _context;

        public Userservice(AsanwebContext context)
        {
            _context = context;
        }

        public bool IsExistUserName(string userName)
        {
            return _context.Users.Any(u => u.UserName == userName);
        }

        public bool IsExistEmail(string email)
        {
            return _context.Users.Any(u => u.Email == email);
        }

        public int AddUser(Users user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return user.UserId;
        }

        public Users LoginUser(LoginViewModel login)
        {
            string password = PasswordEncript.EncriptPassword(login.Password);
            string email = FixedTexd.FixedEmail(login.Email);
            return _context.Users.SingleOrDefault(u => u.Email == email && u.Password == password);
        }

        public bool ActiveCodeAccount(string activeCode)
        {
            var user = _context.Users.SingleOrDefault(u => u.ActiveCode == activeCode);
            if (user == null || user.IsActive)
                return false;
            user.IsActive = true;
            user.ActiveCode = CreateUniqCode.buildUniqCod();
            _context.SaveChanges();
            return true;
        }

        public Users GetUserByEmail(string email)
        {
            return _context.Users.SingleOrDefault(u => u.Email == email);
        }

        public Users GetUserActiveCode(string activeCode)
        {
            return _context.Users.SingleOrDefault(u => u.ActiveCode == activeCode);
        }

        public void UpdateUser(Users user)
        {
            _context.Update(user);
            _context.SaveChanges();
        }

        public Users GetUserByUserName(string userName)
        {
            return _context.Users.SingleOrDefault(u => u.UserName == userName);
        }

        public int GetUserIdByUsername(string userName)
        {
            return _context.Users.Single(u => u.UserName == userName).UserId;
        }

        public Users GetUserByUserId(int userId)
        {
            return _context.Users.Find(userId);
        }

        public InformationUserPanelViewModel GetUserbyUserPanel(string userName)
        {
            var user = GetUserByUserName(userName);
            InformationUserPanelViewModel information = new InformationUserPanelViewModel();
            information.UserName = user.UserName;
            information.Email = user.Email;
            information.RegisterDate = user.RegisterDate;
            information.Wallet = BallenceWallet(userName);
            return information;
        }

        public InformationUserPanelViewModel GetUserbyUserPanel(int userId)
        {
            var user = GetUserByUserId(userId);
            InformationUserPanelViewModel information = new InformationUserPanelViewModel();
            information.UserName = user.UserName;
            information.Email = user.Email;
            information.RegisterDate = user.RegisterDate;
            information.Wallet = BallenceWallet(user.UserName);
            return information;
        }

        public SidbarViewModel GetInformationBySidbar(string userName)
        {
            return _context.Users.Where(u => u.UserName == userName).Select(u => new SidbarViewModel()
            {
                UserName = u.UserName,
                registerDate = u.RegisterDate,
                Image = u.UserImage,
            }).Single();
        }

        public EditProfileViewModel GetUserForEditeProfile(string userName)
        {
            return _context.Users.Where(u => u.UserName == userName).Select(u => new EditProfileViewModel()
            {
                Email = u.Email,
                UserName = u.UserName,
                ImageName = u.UserImage
            }).Single();
        }

        public void EditUser(string userName, EditProfileViewModel profile)
        {
            string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/UserPanel", profile.ImageName);
            if (profile.ImageUser != null)
            {
                if (profile.ImageName != "Defult.jpg")
                {
                    if (File.Exists(imagePath))
                    {
                        File.Delete(imagePath);
                    }
                }
                //Image Name
                profile.ImageName = CreateUniqCode.buildUniqCod() + Path.GetExtension(profile.ImageUser.FileName);
                //Image Path
                imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/UserPanel", profile.ImageName);

                //save
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    profile.ImageUser.CopyTo(stream);
                }
                var user = GetUserByUserName(userName);
                user.Email = profile.Email;
                user.UserName = profile.UserName;
                user.UserImage = profile.ImageName;
                UpdateUser(user);
            }
        }

        public bool CompareOldPassword(string userName, string oldPassword)
        {
            string encriptPassword = PasswordEncript.EncriptPassword(oldPassword);
            return _context.Users.Any(u => u.UserName == userName && u.Password == encriptPassword);
        }

        public void ChangePassword(string userName, string newPassword)
        {
            var user = GetUserByUserName(userName);
            user.Password = PasswordEncript.EncriptPassword(newPassword);
            UpdateUser(user);
        }

        public int BallenceWallet(string userName)
        {
            int userId = GetUserIdByUsername(userName);

            //Variz
            var Diposit = _context.Wallets.Where(W => W.UserId == userId && W.TypeId == 1 && W.IsPay).Select(w => w.Amount)
                .ToList();

            //Bardasht
            var Harvest = _context.Wallets.Where(W => W.UserId == userId && W.TypeId == 2).Select(w => w.Amount)
                .ToList();
            return (Diposit.Sum() - Harvest.Sum());
        }

        public List<WalletInformationViewModel> GetInformationeWallet(string userName)
        {
            int userId = GetUserIdByUsername(userName);
            return _context.Wallets.Where(w => w.UserId == userId && w.IsPay).Select(w =>
               new WalletInformationViewModel()
               {
                   Amount = w.Amount,
                   CreateDate = w.CreateDate,
                   Description = w.Description,
                   TypeId = w.TypeId,
                   WalletId = w.WalletId
               }).ToList();
        }

        public int ChargeWallet(string userName, int amount, string description, bool isPay = false)
        {
            Wallet wallet = new Wallet()
            {
                Amount = amount,
                CreateDate = DateTime.Now,
                IsPay = isPay,
                Description = description,
                TypeId = 1,
                UserId = GetUserIdByUsername(userName),
            };
            return AddWallet(wallet);
        }

        public int AddWallet(Wallet wallet)
        {
            _context.Wallets.Add(wallet);
            _context.SaveChanges();
            return wallet.WalletId;
        }

        public Wallet GetWalletByWalletId(int walletId)
        {
            return _context.Wallets.Find(walletId);
        }

        public void UpdateWallet(Wallet wallet)
        {
            _context.Wallets.Update(wallet);
            _context.SaveChanges();
        }

        public void DeleteWallet(int walletId)
        {
            var wallets = GetWalletByWalletId(walletId);
            wallets.IsDelete = true;
            _context.Wallets.Update(wallets);
            _context.SaveChanges();
        }

        public AdminUserViewModel GetAdminUser(int pageId = 1, string filterUserName = "", string filterEmail = "")
        {
            IQueryable<Users> result = _context.Users;

            //name
            if (!string.IsNullOrEmpty(filterUserName))
            {
                result = result.Where(u => u.UserName.Contains(filterUserName));
            }
            //Email
            if (!string.IsNullOrEmpty(filterEmail))
            {
                result = result.Where(u => u.Email.Contains(filterEmail));
            }

            int take = 8;
            int skip = (pageId - 1) * take;

            AdminUserViewModel list = new AdminUserViewModel();
            list.CurrentPage = pageId;
            list.userse = result.OrderBy(u => u.RegisterDate).Skip(skip).Take(take).ToList();
            list.PageCount = result.Count() / take;
            return list;
        }

        public AdminUserViewModel GetDeleteUser(int pageId = 1, string filterUserName = "", string filterEmail = "")
        {
            IQueryable<Users> result = _context.Users.IgnoreQueryFilters().Where(u => u.IsDelete);

            //name
            if (!string.IsNullOrEmpty(filterUserName))
            {
                result = result.Where(u => u.UserName.Contains(filterUserName));
            }
            //Email
            if (!string.IsNullOrEmpty(filterEmail))
            {
                result = result.Where(u => u.Email.Contains(filterEmail));
            }

            int take = 4;
            int skip = (pageId - 1) * take;

            AdminUserViewModel list = new AdminUserViewModel();
            list.CurrentPage = pageId;
            list.userse = result.OrderBy(u => u.RegisterDate).Skip(skip).Take(take).ToList();
            list.PageCount = result.Count() / take;
            return list;
        }

        public int AddUserForAdmin(CreateNewUserViewModel adminUser)
        {
            Users myUsers = new Users();
            myUsers.UserName = adminUser.UserName;
            myUsers.Email = FixedTexd.FixedEmail(adminUser.Email);
            myUsers.Password = PasswordEncript.EncriptPassword(adminUser.Password);
            myUsers.ActiveCode = CreateUniqCode.buildUniqCod();
            myUsers.RegisterDate = DateTime.Now;
            myUsers.IsActive = true;

            #region Image Admin

            string imagePath = "";
            if (myUsers.UserImage == null)
            {
                myUsers.UserImage = "Defult.jpg";
            }
            else
            {
                //name image
                myUsers.UserImage = CreateUniqCode.buildUniqCod() + Path.GetExtension(adminUser.UserImage.FileName);
                //path image
                imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/UserPanel/", myUsers.UserImage);
                //save
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    adminUser.UserImage.CopyTo(stream);
                }
            }

            #endregion Image Admin

            return AddUser(myUsers);
        }

        public EditAdminViewModel GetInfoAdminForShowEdit(int userId)
        {
            return _context.Users.Where(u => u.UserId == userId).Select(u => new EditAdminViewModel()
            {
                Email = u.Email,
                UserName = u.UserName,
                ImageName = u.UserImage,
                UserId = u.UserId,
                UserRole = u.UserRoles.Select(r => r.RoleId).ToList()
            }).Single();
        }

        public void EditUserForAdmin(EditAdminViewModel editAdmin)
        {
            Users user = GetUserByUserId(editAdmin.UserId);
            //email
            user.Email = editAdmin.Email;

            //password
            if (!string.IsNullOrEmpty(editAdmin.Password))
            {
                user.Password = PasswordEncript.EncriptPassword(editAdmin.Password);
            }

            #region Edit ImageUser

            if (editAdmin.UserImage != null)
            {
                if (editAdmin.ImageName != "Defult.jpg")
                {
                    string deletePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/UserPanel", editAdmin.ImageName);

                    if (File.Exists(deletePath))
                    {
                        File.Delete(deletePath);
                    }
                }

                //Image Name
                user.UserImage = CreateUniqCode.buildUniqCod() + Path.GetExtension(editAdmin.UserImage.FileName);

                //Image Path
                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/UserPanel", user.UserImage);

                //save
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    editAdmin.UserImage.CopyTo(stream);
                }
            }

            #endregion Edit ImageUser

            //ImageUser
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void DeleteUser(int userId)
        {
            Users user = GetUserByUserId(userId);
            user.IsDelete = true;
            UpdateUser(user);
        }

        public void AddContactUs(ContactUs contact)
        {
            _context.ContactUs.Add(contact);
            _context.SaveChanges();
        }

        public ContactUsViewModel GetContactUs(int pageId = 1, string filterUserName = "", string filterEmail = "")
        {
            IQueryable<ContactUs> result = _context.ContactUs.IgnoreQueryFilters().Where(u => !u.IsDelete);

            //name
            if (!string.IsNullOrEmpty(filterUserName))
            {
                result = result.Where(u => u.FullName.Contains(filterUserName));
            }
            //Email
            if (!string.IsNullOrEmpty(filterEmail))
            {
                result = result.Where(u => u.Email.Contains(filterEmail));
            }

            int take = 8;
            int skip = (pageId - 1) * take;

            ContactUsViewModel list = new ContactUsViewModel();

            list.CurrentPage = pageId;
            list.Contact = result.Skip(skip).Take(take).ToList();
            list.PageCount = result.Count() / take;
            return list;
        }

        public ContactUs GetContactIdByContact(int contactId)
        {
            return _context.ContactUs.Find(contactId);
        }

        public void DeleteContactUs(int contactId)
        {
            ContactUs contactUs = GetContactIdByContact(contactId);
            contactUs.IsDelete = true;
            UpdateContact(contactUs);
        }

        public void UpdateContact(ContactUs contactUs)
        {
            _context.ContactUs.Update(contactUs);
            _context.SaveChanges();
        }

        public List<SubscribeToNewsLetter> GetSubscribeByShow()
        {
            return _context.SubscribeToNewsLetter.ToList();
        }

        public void AddSubscribe(SubscribeToNewsLetter subscribe)
        {
            _context.SubscribeToNewsLetter.Add(subscribe);
            _context.SaveChanges();
        }

        public Users LoginExternal(string email)
        {
            return _context.Users.SingleOrDefault(u => u.Email == email);
        }
    }
}