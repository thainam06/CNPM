using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Data;

public static class DemoPlacePhotos
{
    public static PlacePhoto[]? For(string name) => name switch {
        "Chợ Bến Thành" => [new(
            "https://upload.wikimedia.org/wikipedia/commons/f/f5/Inside_Ben_Thanh_market.jpg",
            "Không gian bên trong Chợ Bến Thành · ảnh năm 2008",
            "Stephan Ridgway / Wikimedia Commons · CC BY 2.0. Cắt khung khi hiển thị.",
            "https://commons.wikimedia.org/wiki/File:Inside_Ben_Thanh_market.jpg")],
        "Bưu điện Trung tâm Sài Gòn" => [new(
            "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d4/20190923_Saigon_Central_Post_Office-1.jpg/1280px-20190923_Saigon_Central_Post_Office-1.jpg",
            "Mặt tiền Bưu điện Trung tâm Sài Gòn · ảnh năm 2019",
            "Balon Greyjoy / Wikimedia Commons · CC0 1.0. Cắt khung khi hiển thị.",
            "https://commons.wikimedia.org/wiki/File:20190923_Saigon_Central_Post_Office-1.jpg", 45)],
        "Dinh Độc Lập" => [new(
            "https://upload.wikimedia.org/wikipedia/commons/4/46/Independence_Palace%28Reunification_Palace%29.JPG",
            "Dinh Độc Lập nhìn từ khuôn viên · ảnh năm 2013",
            "Rudolph.A.Furtado / Wikimedia Commons · CC0 1.0; bản nguồn đã cắt bởi Nkon21, cắt khung khi hiển thị.",
            "https://commons.wikimedia.org/wiki/File:Independence_Palace(Reunification_Palace).JPG")],
        "Phố đi bộ Nguyễn Huệ" => [new(
            "https://upload.wikimedia.org/wikipedia/commons/9/9e/04-12-24_Nguyen_Hue_Street%2C_Ho_Chi_Minh_City.jpg",
            "Đài phun nước tại khu vực Nguyễn Huệ – Lê Lợi về đêm · chụp 04/12/2024",
            "Xuanphuocle / Wikimedia Commons · CC0 1.0. Cắt khung khi hiển thị.",
            "https://commons.wikimedia.org/wiki/File:04-12-24_Nguyen_Hue_Street,_Ho_Chi_Minh_City.jpg", 50)],
        "Nhà hát Thành phố" => [new(
            "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f3/Saigon_Opera_House_20190921-1.jpg/1280px-Saigon_Opera_House_20190921-1.jpg",
            "Nhà hát Thành phố về đêm · ảnh năm 2019",
            "Suicasmo / Wikimedia Commons · CC BY-SA 4.0. Cắt khung khi hiển thị.",
            "https://commons.wikimedia.org/wiki/File:Saigon_Opera_House_20190921-1.jpg", 40)],
        _ => null
    };
}
