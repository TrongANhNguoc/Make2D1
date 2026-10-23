using System;

// Giao diện quảng cáo có thưởng. Hiện dùng FakeRewardedAd (video giả); sau này viết lớp mới cho Unity Ads / AdMob
// implement interface này và kéo vào ô `rewardedAdProvider` của DeathUI, không cần sửa DeathUI.
public interface IRewardedAd
{
    // Hiện quảng cáo; gọi onCompleted khi người chơi xem xong và được nhận thưởng
    void Show(Action onCompleted);
}
