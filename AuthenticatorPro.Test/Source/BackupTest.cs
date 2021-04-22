using System.IO;
using AuthenticatorPro.Shared.Data.Backup;
using Newtonsoft.Json;
using Xunit;

namespace AuthenticatorPro.Test
{
    public class BackupTest
    {
        private readonly BackupComparer _backupComparer;
        private readonly Backup _testBackup;
        
        public BackupTest()
        {
            _backupComparer = new BackupComparer();
            
            var contents = File.ReadAllText("test.authpro");
            _testBackup = JsonConvert.DeserializeObject<Backup>(contents);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("t")]
        [InlineData("test")]
        [InlineData("test123")]
        [InlineData("test123!?%")]
        [InlineData("PZqE=_L]Ra;ZD8N&")]
        [InlineData("tUT.3raAGQ[f]]Q@Ft=S}.r(Vk&CM9#`")]
        [InlineData(@"MS^NqdNp&y]tLz_5:P;UU/2LDd_uF7a""x@*a't/Da]'y&b~.=&z3x'r^u{X.@?vv")]
        [InlineData(@"p[{(]2QFSYWcaYdz=;eMtrnZ<bvh8QfW;8v""4HBTtW5H!xMGQKt^\\)f]7.fJ*9dcs@pq(9GKF?7FJ3Qtj$].V!U;:N^/eUj(zG;yC9/pPRGT'DHLR>S5h$}L\\AL~X,pA")]
        [InlineData(@"#n*rF;y3zVPr,B3*qP""C,S[NFk""qswBC;k2*E_gQ&)4Rh.h[f;WG)5w3uY7MGvM/wngR]72BP9;{)_~+Vd""ukx'S]Zt[!cE='43f}J:J_TR<\:`w""{\+`dgdy%vD]Ts:xKAXzgRW!_7vWF]zg*u.)F#ZzzY&[LEHFXH(D@M7Y""'6.e7n~u""[4kyc'TGF28Q""xgNg.!5=;Hfx'e)fx,:#mLmkA(*ty]4]7#;^?*QF4xDK&Fx-)f}(ph=PL*N'#w9`")]
        [InlineData("你好世界")]
        [InlineData("😀 😃 😄 😁 😆 😅 😂 🤣")]
        public void ToBytesFromBytesTest(string password)
        {
            var transformed = Backup.FromBytes(_testBackup.ToBytes(password), password);
            Assert.Equal(transformed, _testBackup, _backupComparer); 
        }
    }
}