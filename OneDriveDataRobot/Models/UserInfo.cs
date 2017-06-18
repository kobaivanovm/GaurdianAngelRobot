namespace OneDriveDataRobot.Directory
{
    using System.Threading.Tasks;
    using Microsoft.Graph;
    public class UserInfo
    {
        public static async Task<User> GetUserInfoAsync(string graphBaseUrl, string userObjectId, string accessToken)
        {
            if (!string.IsNullOrEmpty(userObjectId))
            {
                return await HttpHelper.Default.GetAsync<User>($"{graphBaseUrl}/v1.0/users/{userObjectId}", accessToken);
            }

            return null;
        }

    }
}
