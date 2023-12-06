using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using TMPro;
using System.Threading.Tasks;
using System;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AuthManager : MonoBehaviour
{
    //Firebase variables
    [Header("Firebase")]
    [SerializeField] private DependencyStatus dependencyStatus;
    [SerializeField] private FirebaseAuth auth;
    [SerializeField] private FirebaseUser User;

    //Login variables
    [Header("Login")]
    [SerializeField] private InputField emailLoginField;
    [SerializeField] private InputField passwordLoginField;

    //Register variables
    [Header("Register")]
    [SerializeField] private InputField usernameRegisterField;
    [SerializeField] private InputField emailRegisterField;
    [SerializeField] private InputField passwordRegisterField;
    [SerializeField] private InputField passwordRegisterVerifyField;
    [SerializeField] private InputField forgetPasswordEmailField;
    [SerializeField] GameObject errorMessage;
    private void Start()
    {
        StartCoroutine(CheckAndFixDependenciesAsync());
    }

    private IEnumerator CheckAndFixDependenciesAsync()
    {
        MenuController.instance.loadingPopupPannel.SetActive(true);
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);
        dependencyStatus = dependencyTask.Result;
        if (dependencyStatus == DependencyStatus.Available)
        {
            //If they are avalible Initialize Firebase
            InitializeFirebase();
            yield return new WaitForEndOfFrame();
            StartCoroutine(CheckForAutoLogin());
        }
        else
        {
            Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
        }
    }

    private void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth");
        //Set the authentication instance object
        auth = FirebaseAuth.DefaultInstance;
       // GameManager.Instance.databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    private void AuthStateChanged(object sender, EventArgs e)
    {
        if (auth.CurrentUser != null)
        {
            bool signedIn = User != auth.CurrentUser && auth.CurrentUser is null;

            if (signedIn && User != null)
            {
                Debug.Log("Signed out - " + User.UserId);
            }

            User = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log("Signed In - " + User.UserId);
            }
        }
    }

    private IEnumerator CheckForAutoLogin()
    {
        Debug.Log("Check for Auto Login");
        if (User != null)
        {
            var reloadUserTask = User.ReloadAsync();
            yield return new WaitUntil(() => reloadUserTask.IsCompleted);
            AutoLogin();
        }
        else
        {
            Debug.Log(" Auto Login Failed");
            MenuController.instance.SetAcitveObject(MenuController.instance.loginScreenPanel);
        }
        //UIManager.instance.DisableLoader();
    }

    private void AutoLogin()
    {
        if (User != null)
        {
            string providerid = string.Empty;
            if (FirebaseAuth.DefaultInstance.CurrentUser != null)
            {
                foreach (Firebase.Auth.IUserInfo d in FirebaseAuth.DefaultInstance.CurrentUser.ProviderData)
                {
                    providerid = d.ProviderId;
                    if (providerid == "facebook.com")
                        break;
                }
            }
           if (User.IsEmailVerified)
            {
                Debug.Log("AutoLogin Successfull");
                MenuController.instance.SetAcitveObject(MenuController.instance.selectionPannel);
                MenuController.instance.userName.text = User.DisplayName;
            }
            else
            {
                SendEmailForVerification();
            }
        }
        else
        {
            Debug.Log(" Auto Login Failed");
            MenuController.instance.SetAcitveObject(MenuController.instance.loginScreenPanel);

        }
    }

    //Function for the login button
    public void LoginButton()
    {
        //Call the login coroutine passing the email and password
        StartCoroutine(Login(emailLoginField.text, passwordLoginField.text));
    }
    //Function for the register button
    public void RegisterButton()
    {
        //Call the register coroutine passing the email, password, and username
        StartCoroutine(Register(emailRegisterField.text, passwordRegisterField.text, usernameRegisterField.text));
    }
    GameObject errorObject;
    IEnumerator ShowMessage(string msg)
    {
        if (errorObject == null)
        {
            errorObject = Instantiate(errorMessage);
            errorObject.transform.SetParent(MenuController.instance.canvas.transform, false);
        }
        errorObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = msg;
        yield return new WaitForSeconds(1);
        if (errorObject != null)
            Destroy(errorObject);

    }
    private IEnumerator Login(string _email, string _password)
    {
        if (!isValidEmail(_email))
        {
           StartCoroutine(ShowMessage("please enter your valid email"));
        }
        else if(string.IsNullOrWhiteSpace(_password))
            {
            StartCoroutine(ShowMessage("please enter your valid password"));
        }
        else {
            MenuController.instance.loadingPopupPannel.SetActive(true);

            //Call the Firebase auth signin function passing the email and password
            Task<AuthResult> LoginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
            //Wait until the task completes
            yield return new WaitUntil(() => LoginTask.IsCompleted);
            //UIManager.instance.DisableLoader();
            if (LoginTask.Exception != null)
            {
                //If there are errors handle them
                Debug.LogWarning(message: $"Failed to register task with {LoginTask.Exception}");
                FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                MenuController.instance.loadingPopupPannel.SetActive(false);
                string message = "Login Failed!";
                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.WrongPassword:
                        message = "Wrong Password";
                        break;
                    case AuthError.InvalidEmail:
                        message = "Invalid Email";
                        break;
                    case AuthError.UserNotFound:
                        message = "Account does not exist";
                        break;
                }
                StartCoroutine(ShowMessage(message));
            }
            else
            {
                //User is now logged in
                //Now get the result
                User = LoginTask.Result.User;
                Debug.LogFormat("User signed in successfully: {0} ({1})", User.DisplayName, User.Email);
                StartCoroutine(ShowMessage("User signed in successfully:"));
                if (User.IsEmailVerified)
                {
                    MenuController.instance.SetAcitveObject(MenuController.instance.selectionPannel);
                    // UserName.text = User.DisplayName;
                    MenuController.instance.userName.text = User.DisplayName;
                  
                }
                else
                {
                    SendEmailForVerification();
                }
            }
        }
    }
   
    

    private IEnumerator SignInWithCredential(Credential credential)
    {
        var authTask = auth.SignInWithCredentialAsync(credential);
        yield return new WaitUntil(() => authTask.IsCompleted);

        if (authTask.IsCanceled)
        {
            MenuController.instance.loadingPopupPannel.SetActive(false);
            StartCoroutine(ShowMessage("Firebase authentication was canceled."));
        }
        else if (authTask.IsFaulted)
        {
            MenuController.instance.loadingPopupPannel.SetActive(false);
            StartCoroutine(ShowMessage("Firebase authentication encountered an error: " + authTask.Exception));
        }
        else if (authTask.IsCompleted)
        {
            FirebaseUser user = auth.CurrentUser;
            if (user != null)
            {
                Debug.Log("User is now authenticated with Firebase: " + user.DisplayName);
                MenuController.instance.SetAcitveObject(MenuController.instance.selectionPannel);
                MenuController.instance.userName.text = user.DisplayName;

                // You can now use this user information as needed in your Unity application.
            }
        }
    }

    public static bool isValidEmail(string inputEmail)
    {
        // email validation code
        string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
              @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
              @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
        Regex re = new Regex(strRegex);
        if (re.IsMatch(inputEmail))
            return (true);
        else
            return (false);
    }
    private IEnumerator Register(string _email, string _password, string _username)
    {
        if (string.IsNullOrWhiteSpace(_username))
        {
            //If the username field is blank show a warning
            StartCoroutine(ShowMessage("please enter your User Name"));
        }
        else if (!isValidEmail(_email))
        {
            StartCoroutine(ShowMessage("please enter your valid email"));
        }
        else if (string.IsNullOrEmpty(_password))
        {

            StartCoroutine(ShowMessage("please enter your password"));
        }
        else if (!passwordRegisterField.text.Equals(passwordRegisterVerifyField.text))
        {
            StartCoroutine(ShowMessage("confirm password is not match"));
        }
        else
        {
            //UIManager.instance.EnableLoader();
            //Call the Firebase auth signin function passing the email and password
            Task<AuthResult> RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);
            //Wait until the task completes
            yield return new WaitUntil(() => RegisterTask.IsCompleted);
            //UIManager.instance.DisableLoader();
            if (RegisterTask.Exception != null)
            {
                MenuController.instance.loadingPopupPannel.SetActive(false);
                //If there are errors handle them
                Debug.LogWarning(message: $"Failed to register task with {RegisterTask.Exception}");
                FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "You are Already Registered";
                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        message = "Email is missing";
                        break;
                    case AuthError.MissingPassword:
                        message = "Password is missing";
                        break;
                    case AuthError.InvalidEmail:
                        message = "Email is invalid!";
                        break;
                    case AuthError.WrongPassword:
                        message = "Password is wrong!";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "You are Already Registered";
                        break;
                    default:
                        message = "Registration failed.";
                        break;
                }
                StartCoroutine(ShowMessage(message));
            }
            else
            {
                //User has now been created
                //Now get the result
                User = RegisterTask.Result.User;

                if (User != null)
                {
                    //Create a user profile and set the username
                    UserProfile profile = new UserProfile { DisplayName = _username };

                    //Call the Firebase auth update user profile function passing the profile with the username
                    Task ProfileTask = User.UpdateUserProfileAsync(profile);
                    //Wait until the task completes
                    yield return new WaitUntil(() => ProfileTask.IsCompleted);

                    if (ProfileTask.Exception != null)
                    {
                        User.DeleteAsync();
                        MenuController.instance.loadingPopupPannel.SetActive(false);
                        //If there are errors handle them
                        Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
                        FirebaseException firebaseEx = ProfileTask.Exception.GetBaseException() as FirebaseException;
                        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                        string message = "You are Already Registered";
                        switch (errorCode)
                        {
                            case AuthError.MissingEmail:
                                message = "Email is missing";
                                break;
                            case AuthError.MissingPassword:
                                message = "Password is missing";
                                break;
                            case AuthError.InvalidEmail:
                                message = "Email is invalid!";
                                break;
                            case AuthError.WrongPassword:
                                message = "Password is wrong!";
                                break;
                            case AuthError.EmailAlreadyInUse:
                                message = "You are Already Registered";
                                break;
                            default:
                                message = "Registration failed.";
                                break;
                        }
                        StartCoroutine(ShowMessage(message));
                    }
                    else
                    {
                        
                        MenuController.instance.userName.text = User.DisplayName;
                        //Username is now set
                        //Now return to login screen
                        Debug.Log("Registration is Successfull");
                        if (User.IsEmailVerified)
                        {
                            MenuController.instance.SetAcitveObject(MenuController.instance.loginScreenPanel);

                        }
                        else
                        {
                            SendEmailForVerification();
                        }
                        StartCoroutine(ShowMessage("Registration is Successfull"));
                    }
                }
            }
        }
    }

    public void SendEmailForVerification()
    {
        StartCoroutine(SendEmailForVerificationAsync());
    }

    private IEnumerator SendEmailForVerificationAsync()
    {
        if (User != null)
        {
            MenuController.instance.loadingPopupPannel.SetActive(true);
            var sendEmailTask = User.SendEmailVerificationAsync();
            yield return new WaitUntil(() => sendEmailTask.IsCompleted);
            //UIManager.instance.DisableLoader();
            if (sendEmailTask.Exception != null)
            {
                MenuController.instance.loadingPopupPannel.SetActive(false);
                FirebaseException firebaseEx = sendEmailTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                string errorMessage = "Unknown Error : Please try again later";
                switch (errorCode)
                {
                    case AuthError.Cancelled:
                        errorMessage = "Email Verification was cancelled";
                        break;
                    case AuthError.TooManyRequests:
                        errorMessage = "Too many requests";
                        break;
                    case AuthError.InvalidRecipientEmail:
                        errorMessage = "The Email you entered is invalid";
                        break;
                }
                StartCoroutine(ShowMessage(errorMessage));
               // MenuController.instance.ShowVerificationResponse(false, User.Email, errorMessage);
            }
            else
            {
                MenuController.instance.loadingPopupPannel.SetActive(false);
                Debug.Log("Email Has Succesfully sent");
               // StartCoroutine(ShowMessage("mail Has Succesfully sent"));too
                // MenuController.instance.ShowVerificationResponse(true, User.Email, null);
                MenuController.instance.SetAcitveObject(MenuController.instance.verfiyScreenPanel);
            }

        }
    }

    public void ForgetPasswordReset()
    {
        StartCoroutine(ForgetPassword(forgetPasswordEmailField.text));
    }

    public IEnumerator ForgetPassword(string emailId)
    {
        if (!isValidEmail(emailId))
        {
            StartCoroutine(ShowMessage("please enter your valid email"));
        }
        else
        {
            MenuController.instance.loadingPopupPannel.SetActive(true);
            var forgetPasswordTask = auth.SendPasswordResetEmailAsync(emailId);
            yield return new WaitUntil(() => forgetPasswordTask.IsCompleted);
            //UIManager.instance.DisableLoader();
            if (forgetPasswordTask.Exception != null)
            {
                MenuController.instance.loadingPopupPannel.SetActive(false);
                FirebaseException firebaseEx = forgetPasswordTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                string errorMessage = "Unknown Error : Please try again later";
                switch (errorCode)
                {
                    case AuthError.Cancelled:
                        errorMessage = "Email Verification was cancelled";
                        StartCoroutine(ShowMessage(errorMessage));
                        break;
                    case AuthError.TooManyRequests:
                        errorMessage = "Too many requests";
                        StartCoroutine(ShowMessage(errorMessage));

                        break;
                    case AuthError.InvalidRecipientEmail:
                        errorMessage = "The Email you entered is invalid";
                        StartCoroutine(ShowMessage(errorMessage));
                        break;
                }
                StartCoroutine(ShowMessage($"Password reset link sent to you Email {emailId}"));
                MenuController.instance.loadingPopupPannel.SetActive(false);
                Debug.Log("Error while reset password -- " + errorMessage);
            }
            else
            {
                MenuController.instance.loadingPopupPannel.SetActive(false);
                StartCoroutine(ShowMessage("Password reset link sent to you Emiail"));
                 MenuController.instance.SetAcitveObject(MenuController.instance.loginScreenPanel);
            }
        }
    }

    public void Logout()
    {
        StartCoroutine(LogoutAsync());
    }

    private IEnumerator LogoutAsync()
    {
        if (auth != null && User != null)
        {
            auth.SignOut();
            //UIManager.instance.EnableLoader();
            var deleteUserTask = User.DeleteAsync();
            yield return new WaitUntil(() => deleteUserTask.IsCompleted);
            //UIManager.instance.DisableLoader();
            MenuController.instance.SetAcitveObject(MenuController.instance.loginScreenPanel);
        }
    }
}
