using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Rooms.Infrastructure
{
    public class Helper
    {
        private readonly Settings _settings;
        public Helper(IOptions<Settings> settings)
        {
            _settings = settings.Value;
        }
        public Settings Settings { get => _settings; }
        public string GetToken(string user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_settings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name, user)
                }),
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                                            SecurityAlgorithms.HmacSha256Signature)
            };
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }
        public async Task<bool> SendMail(string to, string content)
        {
            var msg = new SendGridMessage();
            msg.SetFrom(new EmailAddress("inveterate.coder@outlook.com", "Rooms"));
            msg.AddTo(to);
            msg.SetSubject("Confirm your Email");
            msg.AddContent(MimeType.Text, content);
            var client = new SendGridClient(_settings.SGKey);
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                return true;
            else return false;
        }
        public bool isRightName(string name)
        {
            if (Regex.IsMatch(name, @"(^\s+|\s+$)"))
                return false;
            return true;
        }
        public bool isRighGrouptName(string name, bool slug = false)
        {
            char ch = slug ? '_' : ' ';
            if (!Regex.IsMatch(name, $@"^[\p{{L}}\d.\-&'{ch}]+$")) return false;
            if (!slug && (name[^1] == ch || name[^1] == '-')) return false;
            if (name[0] == '.' || name[0] == ch || name[0] == '-' || name[0] == '\'' || name[0] =='&') return false;
            bool verify(string marks)
            {
                for (var i = marks.Length - 1; i >= 0; i--)
                {
                    switch (marks[i])
                    {
                        case '\'':
                        case '.':
                            if (i > 0) return false;
                            break;
                        case '-':
                            if ((i > 0 && marks[i - 1] != ch) || (i > 1 && marks[i - 2] == '-'))
                                return false;
                            break;
                        case ' ':
                        case '_':
                            if (i > 0 && marks[i - 1] == ch) return false;
                            break;
                    }
                }
                return true;
            }
            var marks = "";
            for (var i = name.Length - 1; i >= 0; i--)
            {
                var cchar = name[i];
                if (cchar == '.' || cchar == ch || cchar == '-' || cchar == '\'' || cchar == '&')
                    marks = cchar + marks;
                else
                {
                    if (marks.Length > 0)
                    {
                        var ret = verify(marks);
                        if (!ret) return ret;
                        else marks = "";
                    }
                }
            }
            return true;
        }
        public string Slugify(string name) => Regex.Replace(name, @"\s", "_").ToLower();
    }
}