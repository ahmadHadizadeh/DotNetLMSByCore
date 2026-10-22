using Core.DTO;
using DataLayer.Entities;
using DataLayer.Entities.User;
using System.Collections.Generic;

namespace Core.Services.Interfaces
{
    public interface IUserService
    {
        #region User

        bool IsExistUserName(string userName);
        bool IsExistEmail(string email);
        int AddUser(Users user);
        Users LoginUser(LoginViewModel login);
        Users LoginExternal(string email);
        bool ActiveCodeAccount(string activeCode);
        Users GetUserByEmail(string email);
        Users GetUserActiveCode(string activeCode);
        void UpdateUser(Users user);
        Users GetUserByUserName(string userName);
        int GetUserIdByUsername(string userName);
        Users GetUserByUserId(int userId);

        #endregion

        #region UserPanel

       
        InformationUserPanelViewModel GetUserbyUserPanel(string userName);
        InformationUserPanelViewModel GetUserbyUserPanel(int userId);
        SidbarViewModel GetInformationBySidbar(string userName);
        EditProfileViewModel GetUserForEditeProfile(string userName);
        void EditUser(string userName, EditProfileViewModel profile);
        bool CompareOldPassword(string userName, string oldPassword);
        void ChangePassword(string userName, string newPassword);

        #endregion

        #region Wallet

        int BallenceWallet(string userName);
        List<WalletInformationViewModel> GetInformationeWallet(string userName);
        int ChargeWallet(string userName, int amount, string description, bool isPay = false);
        int AddWallet(Wallet wallet);
        Wallet GetWalletByWalletId(int walletId);
        void UpdateWallet(Wallet wallet);
        void DeleteWallet(int walletId);
        #endregion

        #region Admin
        AdminUserViewModel GetAdminUser(int pageId = 1, string filterUserName = "", string filterEmail = "");
        AdminUserViewModel GetDeleteUser(int pageId = 1, string filterUserName = "", string filterEmail = "");
        int AddUserForAdmin(CreateNewUserViewModel adminUser);
        EditAdminViewModel GetInfoAdminForShowEdit(int userId);
        void EditUserForAdmin(EditAdminViewModel editAdmin);
        void DeleteUser(int userId);

        #endregion

        #region ContactUs
        void AddContactUs(ContactUs contact);
        ContactUsViewModel GetContactUs(int pageId = 1, string filterUserName = "", string filterEmail = "");
        ContactUs GetContactIdByContact(int contactId);
        void DeleteContactUs(int contactId);
        void UpdateContact(ContactUs contactUs);
        #endregion

        #region SubscribeToNews

        List<SubscribeToNewsLetter> GetSubscribeByShow();
        void AddSubscribe(SubscribeToNewsLetter subscribe);

        #endregion
    }
}
