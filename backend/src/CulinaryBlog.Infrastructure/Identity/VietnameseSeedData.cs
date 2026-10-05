using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Infrastructure.Identity;

internal static partial class VietnameseSeedData
{
    public static IReadOnlyList<SeedAuthor> Authors { get; } =
    [
        new("phase8-author-01@example.test", "Nguyễn Minh Anh", "Yêu bếp Việt và chuyên ghi chép các món ăn gia đình miền Bắc."),
        new("phase8-author-02@example.test", "Trần Hoàng Nam", "Chia sẻ món ngon miền Trung với cách nấu rõ ràng, dễ làm tại nhà."),
        new("phase8-author-03@example.test", "Lê Thảo Vy", "Đam mê ẩm thực Nam Bộ, các món bánh và món ăn đường phố Việt Nam."),
        new("phase8-author-04@example.test", "Phạm Gia Huy", "Thích tìm hiểu kỹ thuật kho, nướng và những món Việt truyền thống."),
        new("phase8-author-05@example.test", "Võ Ngọc Mai", "Ưu tiên món chay, món rau và bữa cơm Việt cân bằng, vừa vị."),
    ];

    public static IReadOnlyList<SeedCategory> Categories { get; } =
    [
        new("mon-nuoc", "Món nước", "Phở, bún, mì và các món nước đặc trưng của nhiều vùng miền Việt Nam."),
        new("com-va-xoi", "Cơm và xôi", "Những món cơm, cơm trộn và xôi quen thuộc trong bữa ăn Việt."),
        new("mon-kho-va-rim", "Món kho và rim", "Món kho, rim đậm đà dùng cùng cơm nóng trong bữa cơm gia đình."),
        new("mon-xao", "Món xào", "Món xào nhanh trên lửa lớn, giữ độ giòn và hương vị tươi của nguyên liệu."),
        new("canh-va-sup", "Canh và súp", "Các món canh thanh mát, cân bằng vị cho mâm cơm Việt."),
        new("mon-cuon-va-goi", "Món cuốn và gỏi", "Món cuốn, gỏi và nộm nhiều rau thơm, hài hòa vị chua ngọt."),
        new("banh-viet", "Bánh Việt", "Các loại bánh mặn truyền thống làm từ gạo, nếp và bột năng."),
        new("mon-nuong", "Món nướng", "Món nướng thơm lửa than với gia vị đặc trưng của ẩm thực Việt."),
        new("mon-chay", "Món chay", "Món chay đủ vị từ đậu hũ, nấm, rau củ và gia vị thuần thực vật."),
        new("mon-an-vat", "Món ăn vặt", "Những món quà chiều và món ăn đường phố được yêu thích."),
        new("mon-trang-mieng", "Món tráng miệng", "Chè và món ngọt Việt Nam dùng sau bữa ăn hoặc trong ngày nóng."),
        new("mon-ham", "Món hầm", "Món hầm chín mềm, nước dùng đậm hương và cần thời gian nấu kỹ."),
        new("hai-san", "Hải sản", "Các món tôm, cua, mực và cá tươi chế biến theo phong vị ven biển Việt Nam."),
        new("dac-san-vung-mien", "Đặc sản vùng miền", "Những món ăn gắn với địa phương và tập quán ẩm thực trên khắp Việt Nam."),
        new("mon-hap", "Món hấp", "Món hấp giữ vị ngọt tự nhiên, ít dầu và thơm hương gừng, sả, lá chanh."),
        new("mon-lau", "Món lẩu", "Các món lẩu quây quần với nước dùng đậm đà, rau và nguyên liệu ăn kèm phong phú."),
        new("mon-chien", "Món chiên", "Món chiên vàng giòn đúng nhiệt độ, cân bằng cùng rau và nước chấm Việt."),
        new("mon-luoc", "Món luộc", "Món luộc thanh nhẹ, chú trọng độ chín vừa và vị ngọt nguyên bản của thực phẩm."),
        new("do-chua-va-muoi", "Đồ chua và món muối", "Dưa, cà và rau củ lên men hoặc ngâm chua dùng kèm bữa cơm Việt."),
        new("do-uong-viet", "Đồ uống Việt", "Các thức uống dân dã từ cà phê, trà, trái cây và nguyên liệu bản địa."),
    ];

    private static IReadOnlyList<SeedRecipe> CoreRecipes { get; } =
    [
        R("pho-bo-ha-noi", "Phở bò Hà Nội", "mon-nuoc", "Nước dùng trong, thơm quế hồi, ăn cùng bánh phở mềm và thịt bò vừa chín tới.", 40, 180, 4, RecipeDifficulty.Hard, N(485, 31, 58, 14, 3, 980),
            [I("Xương ống bò", 1.5m, "kg"), I("Bánh phở tươi", 600, "g"), I("Thịt bò thăn", 400, "g"), I("Hành tây", 1, "củ"), I("Gừng", 50, "g"), I("Hoa hồi", 4, "cánh"), I("Quế thanh", 1, "thanh"), I("Nước mắm", 3, "muỗng canh"), I("Hành lá và rau mùi", 80, "g")],
            [S("Sơ chế xương", "Chần xương bò 5 phút, rửa sạch. Nướng hành tây và gừng đến khi xém thơm.", 20), S("Nấu nước dùng", "Hầm xương với 4 lít nước, hành, gừng, hồi và quế; hớt bọt thường xuyên để nước trong.", 150), S("Nêm nước phở", "Lọc nước dùng, nêm nước mắm và muối; giữ sôi nhẹ.", 10), S("Hoàn thiện", "Chần bánh phở, xếp thịt bò thái mỏng, chan nước dùng thật nóng rồi thêm hành và rau mùi.", 10)]),

        R("bun-bo-hue", "Bún bò Huế", "mon-nuoc", "Nước dùng bò sả cay thơm, kết hợp giò heo, chả Huế và sợi bún tròn đặc trưng.", 45, 150, 6, RecipeDifficulty.Hard, N(560, 34, 62, 20, 4, 1150),
            [I("Xương bò", 1.2m, "kg"), I("Bắp bò", 600, "g"), I("Giò heo", 600, "g"), I("Bún sợi lớn", 1.2m, "kg"), I("Sả", 8, "cây"), I("Mắm ruốc Huế", 2, "muỗng canh"), I("Dầu điều", 2, "muỗng canh"), I("Chả Huế", 300, "g"), I("Rau sống", 400, "g")],
            [S("Sơ chế thịt", "Chần xương, bắp bò và giò heo rồi rửa sạch. Đập dập sả và bó gọn.", 20), S("Hầm nước dùng", "Hầm xương, bắp bò, giò heo và sả; vớt từng loại thịt khi vừa mềm.", 120), S("Nêm vị Huế", "Hòa mắm ruốc với nước, gạn phần trong vào nồi; thêm dầu điều, ớt và nước mắm.", 15), S("Trình bày", "Cho bún, thịt thái lát, giò và chả vào tô, chan nước dùng rồi dùng với rau sống.", 10)]),

        R("bun-rieu-cua", "Bún riêu cua", "mon-nuoc", "Bún riêu vị chua dịu từ cà chua, riêu cua đồng béo và đậu hũ chiên vàng.", 35, 60, 4, RecipeDifficulty.Medium, N(430, 24, 55, 13, 5, 920),
            [I("Cua đồng xay", 500, "g"), I("Bún tươi", 800, "g"), I("Cà chua", 5, "quả"), I("Đậu hũ", 3, "miếng"), I("Trứng gà", 2, "quả"), I("Mắm tôm", 1, "muỗng canh"), I("Giấm bỗng", 3, "muỗng canh"), I("Hành lá và tía tô", 80, "g")],
            [S("Lọc cua", "Hòa cua xay với 1,5 lít nước, bóp kỹ rồi lọc bỏ xác.", 15), S("Nấu riêu", "Đun nước cua lửa vừa, gạt riêu nổi sang một bên; trộn một phần riêu với trứng để riêu chắc hơn.", 15), S("Nấu nước dùng", "Xào cà chua, cho vào nồi cùng đậu hũ; nêm mắm tôm, giấm bỗng và nước mắm.", 20), S("Hoàn thiện", "Cho bún vào tô, xếp riêu và đậu hũ, chan nước dùng rồi thêm hành lá, tía tô.", 5)]),

        R("hu-tieu-nam-vang", "Hủ tiếu Nam Vang", "mon-nuoc", "Nước dùng xương trong ngọt, dùng với tôm, thịt heo, trứng cút và hủ tiếu dai.", 40, 100, 6, RecipeDifficulty.Hard, N(510, 30, 66, 13, 3, 1050),
            [I("Xương heo", 1.2m, "kg"), I("Hủ tiếu khô", 700, "g"), I("Tôm sú", 400, "g"), I("Thịt nạc vai", 400, "g"), I("Thịt heo xay", 250, "g"), I("Trứng cút", 18, "quả"), I("Củ cải trắng", 300, "g"), I("Hẹ và cần tàu", 100, "g")],
            [S("Nấu nước dùng", "Chần xương rồi hầm với củ cải; hớt bọt để nước trong.", 90), S("Chuẩn bị phần ăn kèm", "Luộc tôm, thịt heo và trứng cút; xào thịt xay với tỏi và chút nước mắm.", 20), S("Trụng hủ tiếu", "Ngâm sợi vừa mềm rồi trụng nhanh trong nước sôi, trộn một ít dầu tỏi.", 5), S("Hoàn thiện", "Xếp tôm, thịt, trứng lên hủ tiếu, chan nước dùng và thêm hẹ, cần tàu.", 5)]),

        R("mi-quang-ga", "Mì Quảng gà", "mon-nuoc", "Sợi mì vàng ăn cùng gà thấm nghệ, ít nước nhân đậm vị, đậu phộng và bánh tráng mè.", 30, 50, 4, RecipeDifficulty.Medium, N(545, 29, 61, 22, 5, 890),
            [I("Mì Quảng", 800, "g"), I("Gà ta", 800, "g"), I("Nghệ tươi", 30, "g"), I("Nén", 20, "g"), I("Đậu phộng rang", 80, "g"), I("Bánh tráng mè", 4, "miếng"), I("Dầu điều", 1, "muỗng canh"), I("Rau sống", 300, "g")],
            [S("Ướp gà", "Chặt gà miếng vừa, ướp nghệ, nén giã, nước mắm, tiêu và dầu điều.", 20), S("Nấu nhân", "Xào gà săn, thêm nước vừa ngập và om đến khi gà mềm, nước còn sánh.", 35), S("Chuẩn bị mì", "Chần nhanh mì Quảng, để ráo; rửa sạch rau sống.", 5), S("Trình bày", "Cho rau và mì vào tô, thêm gà cùng một ít nước nhân, rắc đậu phộng và bánh tráng bẻ.", 5)]),

        R("bun-ca-nha-trang", "Bún cá Nha Trang", "mon-nuoc", "Nước dùng cá thanh ngọt với chả cá dai, cà chua và dứa tạo vị dịu nhẹ.", 35, 70, 4, RecipeDifficulty.Medium, N(390, 27, 52, 9, 4, 840),
            [I("Xương cá thu", 800, "g"), I("Chả cá Nha Trang", 500, "g"), I("Bún tươi", 800, "g"), I("Cà chua", 3, "quả"), I("Dứa", 300, "g"), I("Hành tím", 4, "củ"), I("Hành lá", 50, "g"), I("Rau sống", 250, "g")],
            [S("Nấu nước cá", "Chần xương cá, nấu với hành tím nướng rồi lọc kỹ lấy nước trong.", 45), S("Hoàn thiện nước dùng", "Xào cà chua, dứa rồi cho vào nước cá; nêm nước mắm và muối.", 15), S("Chuẩn bị chả cá", "Chiên hoặc hấp chả cá, cắt lát vừa ăn.", 10), S("Trình bày", "Cho bún và chả cá vào tô, chan nước dùng, thêm hành lá và dùng với rau sống.", 5)]),

        R("banh-canh-cua", "Bánh canh cua", "mon-nuoc", "Sợi bánh canh bột năng mềm dai trong nước cua sánh, có tôm và trứng cút.", 35, 55, 4, RecipeDifficulty.Medium, N(520, 30, 67, 15, 3, 1080),
            [I("Bánh canh bột năng", 800, "g"), I("Cua biển", 2, "con", "khoảng 700 g"), I("Tôm sú", 300, "g"), I("Trứng cút", 12, "quả"), I("Xương heo", 600, "g"), I("Bột năng", 3, "muỗng canh"), I("Dầu điều", 1, "muỗng canh"), I("Hành lá và ngò", 60, "g")],
            [S("Nấu nước dùng", "Hầm xương heo lấy nước ngọt, lọc bỏ xương và cặn.", 40), S("Chuẩn bị hải sản", "Luộc cua và tôm vừa chín; gỡ thịt cua, bóc vỏ tôm.", 15), S("Nấu nước cua", "Xào thịt cua với dầu điều, cho vào nước dùng; thêm bột năng pha loãng đến độ sánh vừa.", 10), S("Hoàn thiện", "Luộc bánh canh, cho vào tô cùng cua, tôm, trứng cút rồi chan nước dùng.", 5)]),

        R("cao-lau-hoi-an", "Cao lầu Hội An", "mon-nuoc", "Sợi cao lầu dai, thịt xá xíu mềm, rau sống Trà Quế và tóp cao lầu giòn.", 40, 70, 4, RecipeDifficulty.Hard, N(540, 28, 63, 20, 6, 960),
            [I("Sợi cao lầu", 700, "g"), I("Thịt ba chỉ", 600, "g"), I("Ngũ vị hương", 1, "thìa cà phê"), I("Nước tương", 3, "muỗng canh"), I("Đường thốt nốt", 2, "muỗng canh"), I("Tóp cao lầu", 120, "g"), I("Giá đỗ", 200, "g"), I("Rau sống", 300, "g")],
            [S("Ướp thịt", "Ướp ba chỉ với ngũ vị hương, nước tương, tỏi, đường và tiêu.", 30), S("Rim thịt", "Áp chảo thịt rồi thêm nước ướp, rim nhỏ lửa đến mềm; để nguội và thái lát.", 50), S("Chuẩn bị sợi", "Chần sợi cao lầu và giá đỗ, để ráo.", 5), S("Trình bày", "Xếp rau, sợi, thịt và tóp giòn; chan một ít nước rim thịt đậm vị.", 5)]),

        R("com-tam-suon-bi-cha", "Cơm tấm sườn bì chả", "com-va-xoi", "Đĩa cơm tấm Sài Gòn đủ sườn nướng, bì, chả trứng, đồ chua và mỡ hành.", 45, 40, 4, RecipeDifficulty.Hard, N(720, 38, 82, 27, 5, 1180),
            [I("Gạo tấm", 500, "g"), I("Sườn cốt lết", 4, "miếng"), I("Bì heo", 200, "g"), I("Thính gạo", 30, "g"), I("Thịt heo xay", 250, "g"), I("Trứng vịt", 4, "quả"), I("Cà rốt và củ cải", 300, "g"), I("Hành lá", 60, "g")],
            [S("Ướp sườn", "Ướp sườn với sả, tỏi, nước mắm, mật ong và dầu ăn ít nhất 30 phút.", 30), S("Làm bì và chả", "Trộn bì luộc thái sợi với thính; hấp hỗn hợp thịt xay, miến và trứng đến chín.", 30), S("Nướng sườn", "Nướng sườn trên than hoặc chảo đến chín, xém cạnh; quét phần nước ướp khi nướng.", 15), S("Hoàn thiện", "Dọn cơm tấm với sườn, bì, chả, đồ chua, mỡ hành và nước mắm pha.", 10)]),

        R("com-ga-hoi-an", "Cơm gà Hội An", "com-va-xoi", "Cơm vàng thơm nghệ nấu bằng nước luộc gà, ăn cùng gà xé và rau răm hành tây.", 35, 55, 4, RecipeDifficulty.Medium, N(610, 36, 68, 21, 4, 870),
            [I("Gà ta", 1.2m, "kg"), I("Gạo", 500, "g"), I("Nghệ tươi", 20, "g"), I("Hành tây", 1, "củ"), I("Rau răm", 50, "g"), I("Đu đủ xanh", 250, "g"), I("Tỏi", 4, "tép"), I("Chanh", 2, "quả")],
            [S("Luộc gà", "Luộc gà với gừng, hành và muối ở lửa nhẹ; ngâm gà trong nước luộc 10 phút rồi để nguội.", 35), S("Nấu cơm", "Xào gạo với nghệ và mỡ gà, nấu bằng nước luộc gà đến chín tơi.", 25), S("Trộn gà", "Xé gà, trộn với hành tây, rau răm, chanh, muối tiêu và chút nước mắm.", 10), S("Trình bày", "Dọn cơm với gà trộn, đu đủ chua và chén nước luộc gà.", 5)]),

        R("com-hen-hue", "Cơm hến Huế", "com-va-xoi", "Cơm nguội trộn hến xào, rau thơm, hoa chuối, đậu phộng và nước hến cay nồng.", 30, 35, 4, RecipeDifficulty.Medium, N(445, 23, 58, 14, 7, 990),
            [I("Hến tươi", 1.5m, "kg"), I("Cơm nguội", 600, "g"), I("Hoa chuối bào", 250, "g"), I("Khế chua", 2, "quả"), I("Đậu phộng rang", 80, "g"), I("Da heo chiên", 100, "g"), I("Mắm ruốc Huế", 1, "muỗng canh"), I("Rau thơm", 150, "g")],
            [S("Luộc hến", "Ngâm hến cho sạch cát, luộc đến mở miệng; đãi lấy thịt và giữ nước luộc trong.", 20), S("Xào hến", "Phi hành, xào hến nhanh với mắm ruốc, ớt và nước mắm.", 8), S("Chuẩn bị rau", "Rửa hoa chuối, khế và rau thơm; rang đậu phộng, chiên da heo giòn.", 10), S("Trộn cơm", "Cho cơm nguội vào tô, thêm hến, rau, khế, đậu phộng và da heo; dùng kèm nước hến nóng.", 5)]),

        R("xoi-gac", "Xôi gấc", "com-va-xoi", "Xôi nếp dẻo thơm, màu đỏ cam tự nhiên từ gấc, thích hợp cho dịp lễ và mâm cỗ.", 20, 45, 6, RecipeDifficulty.Easy, N(390, 7, 70, 9, 3, 120),
            [I("Gạo nếp", 700, "g"), I("Thịt gấc", 250, "g"), I("Nước cốt dừa", 150, "ml"), I("Đường", 80, "g"), I("Muối", 1, "thìa cà phê"), I("Dầu ăn", 1, "muỗng canh")],
            [S("Ngâm nếp", "Vo nếp, ngâm 6–8 giờ rồi để thật ráo.", 480), S("Trộn gấc", "Bóp thịt gấc với một ít rượu trắng, trộn đều cùng nếp và muối.", 10), S("Đồ xôi", "Hấp nếp 30 phút, rưới nước cốt dừa rồi hấp thêm đến khi hạt nếp dẻo trong.", 45), S("Hoàn thiện", "Trộn đường và dầu khi xôi còn nóng, xới nhẹ để hạt nếp bóng mà không nát.", 5)]),

        R("xoi-man", "Xôi mặn", "com-va-xoi", "Xôi dẻo phủ lạp xưởng, gà xé, chà bông, trứng cút, hành phi và mỡ hành.", 25, 45, 4, RecipeDifficulty.Medium, N(590, 22, 72, 24, 3, 940),
            [I("Gạo nếp", 500, "g"), I("Lạp xưởng", 200, "g"), I("Ức gà", 250, "g"), I("Trứng cút", 12, "quả"), I("Chà bông", 80, "g"), I("Hành tím", 6, "củ"), I("Hành lá", 50, "g"), I("Nước tương", 2, "muỗng canh")],
            [S("Đồ xôi", "Ngâm nếp qua đêm, để ráo rồi hấp đến dẻo mềm.", 40), S("Chuẩn bị đồ mặn", "Luộc và xé gà; áp chảo lạp xưởng, luộc trứng cút; phi hành tím vàng.", 20), S("Nêm xôi", "Trộn xôi nóng với mỡ hành và một ít nước tương cho vừa vị.", 5), S("Trình bày", "Xếp lạp xưởng, gà, trứng cút, chà bông và hành phi lên xôi.", 5)]),

        R("com-chien-hai-san", "Cơm chiên hải sản", "com-va-xoi", "Cơm nguội chiên tơi cùng tôm, mực, trứng và rau củ, thơm mùi hành lá.", 20, 15, 4, RecipeDifficulty.Easy, N(520, 25, 67, 17, 4, 760),
            [I("Cơm nguội", 700, "g"), I("Tôm", 250, "g"), I("Mực", 250, "g"), I("Trứng gà", 3, "quả"), I("Cà rốt", 100, "g"), I("Đậu Hà Lan", 100, "g"), I("Hành lá", 50, "g"), I("Nước mắm", 1, "muỗng canh")],
            [S("Chuẩn bị", "Làm sạch tôm mực, cắt vừa ăn; đánh trứng, cắt hạt lựu cà rốt.", 15), S("Xào hải sản", "Làm nóng chảo, xào tôm mực vừa chín rồi trút ra.", 4), S("Chiên cơm", "Đảo trứng, thêm cơm và rau củ, chiên lửa lớn đến hạt cơm tơi.", 8), S("Hoàn thiện", "Cho hải sản lại chảo, nêm nước mắm và tiêu, đảo cùng hành lá.", 3)]),

        R("ca-kho-to", "Cá kho tộ", "mon-kho-va-rim", "Cá lóc kho trong nồi đất với nước màu, nước mắm và tiêu đến khi thấm đậm.", 20, 45, 4, RecipeDifficulty.Medium, N(330, 31, 9, 18, 1, 980),
            [I("Cá lóc", 900, "g"), I("Thịt ba chỉ", 150, "g"), I("Nước dừa tươi", 300, "ml"), I("Nước mắm", 4, "muỗng canh"), I("Đường", 2, "muỗng canh"), I("Hành tím", 4, "củ"), I("Tiêu đen", 1, "thìa cà phê"), I("Ớt", 2, "quả")],
            [S("Ướp cá", "Cắt cá thành khoanh, ướp nước mắm, hành tím, tiêu và một phần đường.", 20), S("Làm nước màu", "Thắng phần đường còn lại đến màu cánh gián, thêm ba chỉ đảo săn.", 5), S("Kho cá", "Xếp cá vào nồi đất, thêm nước dừa; kho lửa nhỏ đến khi nước sánh và cá thấm.", 40), S("Hoàn thiện", "Rắc thêm tiêu, hành lá và ớt, dùng nóng với cơm.", 2)]),

        R("thit-kho-trung", "Thịt kho trứng", "mon-kho-va-rim", "Thịt ba chỉ mềm trong, trứng thấm nước dừa và nước mắm, vị mặn ngọt hài hòa.", 25, 90, 6, RecipeDifficulty.Medium, N(505, 28, 14, 37, 1, 1030),
            [I("Thịt ba chỉ", 1, "kg"), I("Trứng vịt", 8, "quả"), I("Nước dừa tươi", 1, "lít"), I("Nước mắm", 5, "muỗng canh"), I("Đường", 3, "muỗng canh"), I("Hành tím", 5, "củ"), I("Tỏi", 4, "tép"), I("Ớt", 2, "quả")],
            [S("Ướp thịt", "Cắt thịt miếng lớn, ướp nước mắm, hành tỏi giã, tiêu và một ít đường.", 30), S("Luộc trứng", "Luộc chín trứng, ngâm nước lạnh rồi bóc vỏ.", 12), S("Kho thịt", "Thắng nước màu, đảo thịt săn; thêm nước dừa và kho lửa nhỏ, thường xuyên hớt bọt.", 60), S("Thêm trứng", "Cho trứng vào kho thêm 25–30 phút để thấm mà lòng trắng không chai.", 30)]),

        R("tom-rim-man-ngot", "Tôm rim mặn ngọt", "mon-kho-va-rim", "Tôm nguyên vỏ rim nước mắm và đường đến bóng đỏ, thịt chắc và thấm vị.", 15, 18, 4, RecipeDifficulty.Easy, N(260, 27, 17, 9, 1, 1010),
            [I("Tôm sú", 700, "g"), I("Nước mắm", 3, "muỗng canh"), I("Đường", 2, "muỗng canh"), I("Tỏi", 5, "tép"), I("Hành tím", 3, "củ"), I("Dầu điều", 1, "thìa cà phê"), I("Tiêu", 0.5m, "thìa cà phê"), I("Hành lá", 30, "g")],
            [S("Sơ chế tôm", "Cắt râu, rút chỉ lưng, rửa tôm và để thật ráo.", 10), S("Tạo màu", "Phi hành tỏi với dầu điều, cho đường vào đảo đến màu hổ phách nhạt.", 3), S("Rim tôm", "Cho tôm và nước mắm vào, đảo lửa vừa đến khi tôm đỏ và nước rim bám quanh vỏ.", 12), S("Hoàn thiện", "Rắc tiêu và hành lá, tắt bếp khi tôm vừa săn để không bị khô.", 2)]),

        R("ga-kho-gung", "Gà kho gừng", "mon-kho-va-rim", "Gà ta kho săn với nhiều gừng thái sợi, nước mắm và tiêu, ấm vị và thơm nồng.", 20, 35, 4, RecipeDifficulty.Easy, N(365, 35, 8, 21, 1, 850),
            [I("Gà ta", 900, "g"), I("Gừng", 100, "g"), I("Nước mắm", 3, "muỗng canh"), I("Đường", 1.5m, "muỗng canh"), I("Hành tím", 4, "củ"), I("Tỏi", 3, "tép"), I("Ớt", 2, "quả"), I("Tiêu", 1, "thìa cà phê")],
            [S("Ướp gà", "Chặt gà miếng vừa, ướp nước mắm, hành tỏi, tiêu và một nửa lượng gừng.", 20), S("Xào săn", "Thắng đường màu cánh gián, cho gà vào đảo lửa vừa đến săn mặt.", 8), S("Kho gà", "Thêm ít nước nóng và phần gừng còn lại, kho lửa nhỏ đến gà mềm và nước cạn sánh.", 25), S("Hoàn thiện", "Nếm lại cho mặn ngọt vừa, thêm ớt và tiêu trước khi dọn.", 2)]),

        R("bo-kho", "Bò kho", "mon-ham", "Bò hầm mềm với sả, hồi, cà rốt và gia vị bò kho, dùng cùng bánh mì hoặc hủ tiếu.", 35, 120, 6, RecipeDifficulty.Hard, N(480, 37, 28, 25, 5, 970),
            [I("Nạm bò", 1.2m, "kg"), I("Cà rốt", 500, "g"), I("Sả", 5, "cây"), I("Gừng", 40, "g"), I("Hoa hồi", 3, "cánh"), I("Bột gia vị bò kho", 2, "muỗng canh"), I("Nước dừa tươi", 700, "ml"), I("Bánh mì", 6, "ổ")],
            [S("Ướp bò", "Cắt bò khối lớn, ướp gia vị bò kho, tỏi, gừng, nước mắm và dầu điều.", 30), S("Xào bò", "Phi sả, cho bò vào xào lửa lớn đến săn đều các mặt.", 10), S("Hầm mềm", "Thêm nước dừa, nước nóng và hoa hồi; hầm nhỏ lửa đến khi bò gần mềm.", 90), S("Thêm cà rốt", "Cho cà rốt vào hầm thêm 20 phút, nêm lại rồi dùng với bánh mì và rau quế.", 20)]),

        R("ca-bong-kho-tieu", "Cá bống kho tiêu", "mon-kho-va-rim", "Cá bống nhỏ kho keo cùng nước mắm và nhiều tiêu, thịt cá chắc, vị đậm đà.", 20, 35, 4, RecipeDifficulty.Medium, N(285, 29, 10, 14, 1, 960),
            [I("Cá bống", 700, "g"), I("Nước mắm", 3, "muỗng canh"), I("Đường", 2, "muỗng canh"), I("Hành tím", 4, "củ"), I("Tiêu sọ", 2, "thìa cà phê"), I("Ớt", 3, "quả"), I("Nước dừa", 250, "ml"), I("Hành lá", 30, "g")],
            [S("Làm sạch cá", "Đánh vảy, bỏ ruột cá bống, rửa nhẹ với nước muối rồi để ráo.", 15), S("Ướp cá", "Ướp cá với nước mắm, hành tím, đường, ớt và một nửa lượng tiêu.", 20), S("Kho cá", "Thắng nước màu, xếp cá vào nồi, thêm nước dừa và kho lửa nhỏ không đảo mạnh.", 30), S("Kho keo", "Mở nắp, kho đến khi nước sánh; rắc tiêu và hành lá.", 5)]),

        R("rau-muong-xao-toi", "Rau muống xào tỏi", "mon-xao", "Rau muống xanh giòn xào nhanh với tỏi vàng, món rau quen thuộc của bữa cơm Việt.", 15, 7, 4, RecipeDifficulty.Easy, N(145, 5, 13, 9, 5, 520),
            [I("Rau muống", 700, "g"), I("Tỏi", 8, "tép"), I("Dầu ăn", 2, "muỗng canh"), I("Nước mắm", 1, "muỗng canh"), I("Dầu hào", 1, "muỗng canh"), I("Đường", 0.5m, "thìa cà phê")],
            [S("Nhặt rau", "Bỏ phần già, ngắt rau dài khoảng 8 cm, rửa sạch và để ráo.", 12), S("Chần rau", "Chần rau 30 giây trong nước sôi có chút muối, vớt ngay vào nước lạnh.", 2), S("Xào nhanh", "Phi thơm tỏi trong chảo rất nóng, cho rau và gia vị vào đảo liên tục.", 3), S("Hoàn thiện", "Tắt bếp khi rau vừa thấm và còn xanh giòn, dùng ngay.", 1)]),

        R("bo-luc-lac", "Bò lúc lắc", "mon-xao", "Thịt bò cắt khối áp chảo xém mặt, xào cùng hành và ớt chuông, dùng với muối tiêu chanh.", 25, 12, 4, RecipeDifficulty.Medium, N(410, 34, 18, 24, 3, 760),
            [I("Thăn bò", 650, "g"), I("Ớt chuông", 2, "quả"), I("Hành tây", 1, "củ"), I("Tỏi", 5, "tép"), I("Dầu hào", 2, "muỗng canh"), I("Nước tương", 1, "muỗng canh"), I("Cà chua", 2, "quả"), I("Xà lách xoong", 150, "g")],
            [S("Ướp bò", "Cắt bò khối 2,5 cm, ướp tỏi, dầu hào, nước tương, đường và tiêu.", 20), S("Áp chảo bò", "Chia bò thành hai mẻ, áp chảo rất nóng đến xém mặt nhưng bên trong còn mọng.", 5), S("Xào rau củ", "Xào nhanh hành tây và ớt chuông, cho bò trở lại đảo đều.", 3), S("Trình bày", "Dọn bò trên xà lách xoong và cà chua, kèm muối tiêu chanh.", 3)]),

        R("mien-xao-cua", "Miến xào cua", "mon-xao", "Miến dong dai mềm xào cùng thịt cua, nấm mèo và rau củ mà không bị bết.", 25, 15, 4, RecipeDifficulty.Medium, N(430, 25, 58, 12, 5, 780),
            [I("Miến dong", 300, "g"), I("Thịt cua", 350, "g"), I("Nấm mèo khô", 25, "g"), I("Cà rốt", 1, "củ"), I("Hành tây", 1, "củ"), I("Hành lá", 50, "g"), I("Nước mắm", 1.5m, "muỗng canh"), I("Dầu ăn", 2, "muỗng canh")],
            [S("Ngâm miến", "Ngâm miến trong nước mát đến vừa mềm, cắt ngắn và trộn chút dầu.", 12), S("Xào cua", "Phi hành, xào thịt cua với nước mắm và tiêu rồi trút ra.", 4), S("Xào miến", "Xào cà rốt, nấm và hành tây; thêm miến cùng ít nước dùng, đảo đến trong mềm.", 7), S("Hoàn thiện", "Cho cua trở lại, đảo cùng hành lá và ngò, nếm vừa vị.", 2)]),

        R("muc-xao-dua", "Mực xào dứa", "mon-xao", "Mực giòn ngọt xào với dứa chua dịu, cà chua, cần tây và hành tây.", 20, 10, 4, RecipeDifficulty.Easy, N(245, 28, 20, 7, 4, 690),
            [I("Mực ống", 600, "g"), I("Dứa", 350, "g"), I("Cà chua", 2, "quả"), I("Cần tây", 100, "g"), I("Hành tây", 1, "củ"), I("Tỏi", 4, "tép"), I("Nước mắm", 1, "muỗng canh"), I("Dầu ăn", 2, "muỗng canh")],
            [S("Sơ chế mực", "Làm sạch, khứa vảy rồng và cắt mực miếng vừa; chần nhanh rồi để ráo.", 12), S("Xào mực", "Phi tỏi trong chảo nóng, xào mực lửa lớn khoảng 90 giây rồi trút ra.", 2), S("Xào rau quả", "Xào dứa, cà chua và hành tây vừa chín, nêm nước mắm và chút đường.", 4), S("Hoàn thiện", "Cho mực và cần tây vào đảo nhanh, tắt bếp khi mực vừa giòn.", 2)]),

        R("canh-chua-ca-loc", "Canh chua cá lóc", "canh-va-sup", "Canh chua Nam Bộ hài hòa me, dứa, cà chua, bạc hà và cá lóc ngọt thịt.", 25, 25, 4, RecipeDifficulty.Medium, N(230, 27, 18, 6, 5, 720),
            [I("Cá lóc", 700, "g"), I("Me chín", 60, "g"), I("Dứa", 250, "g"), I("Cà chua", 3, "quả"), I("Đậu bắp", 150, "g"), I("Bạc hà", 200, "g"), I("Giá đỗ", 150, "g"), I("Ngò om và ngò gai", 50, "g")],
            [S("Sơ chế", "Làm sạch cá, cắt khoanh; cắt dứa, cà chua, đậu bắp và bạc hà vừa ăn.", 20), S("Nấu nước chua", "Dầm me với nước nóng, lọc lấy nước; đun cùng 1,5 lít nước và dứa.", 8), S("Nấu cá", "Cho cá và cà chua vào, nấu nhẹ đến khi cá chín; nêm nước mắm, đường và muối.", 10), S("Hoàn thiện", "Thêm đậu bắp, bạc hà, giá; sôi lại thì tắt bếp, rắc ngò om và ngò gai.", 5)]),

        R("canh-kho-qua-nhoi-thit", "Canh khổ qua nhồi thịt", "canh-va-sup", "Khổ qua nhồi thịt băm và nấm mèo, hầm trong nước dùng trong đến mềm vừa.", 35, 45, 6, RecipeDifficulty.Medium, N(210, 17, 11, 12, 5, 650),
            [I("Khổ qua", 5, "quả"), I("Thịt heo xay", 450, "g"), I("Nấm mèo", 20, "g"), I("Miến", 30, "g"), I("Hành tím", 3, "củ"), I("Nước mắm", 2, "muỗng canh"), I("Hành lá", 50, "g"), I("Nước dùng xương", 2, "lít")],
            [S("Chuẩn bị khổ qua", "Cắt khổ qua thành khúc, lấy sạch ruột và chần nhanh với nước muối.", 15), S("Trộn nhân", "Trộn thịt xay với nấm mèo, miến, hành tím, nước mắm và tiêu.", 10), S("Nhồi và hầm", "Nhồi nhân vừa chặt, xếp vào nước dùng đang sôi nhẹ và hầm đến mềm.", 40), S("Hoàn thiện", "Hớt bọt để nước trong, nêm vừa vị và thêm hành lá trước khi dùng.", 5)]),

        R("canh-cua-rau-day", "Canh cua rau đay", "canh-va-sup", "Canh cua đồng với rau đay, mồng tơi và mướp, vị thanh ngọt dân dã miền Bắc.", 30, 20, 4, RecipeDifficulty.Medium, N(165, 14, 13, 7, 6, 510),
            [I("Cua đồng xay", 400, "g"), I("Rau đay", 250, "g"), I("Mồng tơi", 250, "g"), I("Mướp hương", 1, "quả"), I("Mắm tôm", 1, "thìa cà phê"), I("Muối", 1, "thìa cà phê")],
            [S("Lọc cua", "Hòa cua xay với 1,2 lít nước, bóp kỹ và lọc qua rây.", 12), S("Nấu riêu cua", "Đun lửa vừa, khuấy nhẹ ban đầu; khi riêu nổi thì gạt sang mép nồi.", 10), S("Thêm rau", "Cho mướp vào trước, sau đó thêm rau đay và mồng tơi.", 5), S("Nêm canh", "Nêm muối và một ít mắm tôm, đun sôi lại rồi tắt bếp.", 2)]),

        R("canh-bi-do-nau-tom", "Canh bí đỏ nấu tôm", "canh-va-sup", "Bí đỏ bùi ngọt nấu cùng tôm tươi, món canh dễ làm và hợp bữa cơm hằng ngày.", 15, 20, 4, RecipeDifficulty.Easy, N(175, 13, 22, 5, 4, 460),
            [I("Bí đỏ", 700, "g"), I("Tôm tươi", 300, "g"), I("Hành tím", 2, "củ"), I("Nước mắm", 1, "muỗng canh"), I("Hành lá", 30, "g"), I("Ngò rí", 20, "g")],
            [S("Sơ chế", "Gọt bí, bỏ hạt và cắt khối; bóc tôm, băm thô một nửa.", 12), S("Xào tôm", "Phi hành tím, xào tôm với chút nước mắm đến săn.", 3), S("Nấu canh", "Thêm 1,2 lít nước, đun sôi rồi cho bí vào nấu đến mềm nhưng không nát.", 15), S("Hoàn thiện", "Nêm vừa vị, thêm hành lá và ngò rí rồi tắt bếp.", 2)]),

        R("canh-rau-ngot-thit-bam", "Canh rau ngót thịt băm", "canh-va-sup", "Rau ngót vò nhẹ nấu với thịt băm, nước canh trong và có vị ngọt tự nhiên.", 15, 12, 4, RecipeDifficulty.Easy, N(155, 14, 9, 7, 5, 480),
            [I("Rau ngót", 500, "g"), I("Thịt heo xay", 250, "g"), I("Hành tím", 2, "củ"), I("Nước mắm", 1, "muỗng canh"), I("Dầu ăn", 1, "thìa cà phê"), I("Tiêu", 0.5m, "thìa cà phê")],
            [S("Sơ chế rau", "Tuốt lá rau ngót, rửa sạch rồi vò nhẹ để rau mềm và thơm.", 10), S("Xào thịt", "Phi hành tím, xào thịt băm tơi với chút nước mắm.", 3), S("Nấu canh", "Thêm 1,2 lít nước, đun sôi; cho rau ngót vào nấu đến vừa mềm.", 6), S("Hoàn thiện", "Nêm lại, rắc tiêu và tắt bếp để rau giữ màu xanh.", 1)]),

        R("goi-cuon-tom-thit", "Gỏi cuốn tôm thịt", "mon-cuon-va-goi", "Cuốn bánh tráng tươi với tôm, thịt ba chỉ, bún và rau, dùng cùng tương đậu phộng.", 35, 20, 4, RecipeDifficulty.Medium, N(310, 21, 39, 9, 5, 590),
            [I("Bánh tráng", 16, "miếng"), I("Tôm", 400, "g"), I("Thịt ba chỉ", 350, "g"), I("Bún tươi", 400, "g"), I("Xà lách", 150, "g"), I("Hẹ", 80, "g"), I("Rau thơm", 120, "g"), I("Tương đen", 120, "ml")],
            [S("Luộc nhân", "Luộc thịt chín, để nguội thái lát; luộc tôm vừa chín, bóc vỏ và chẻ đôi.", 20), S("Chuẩn bị rau bún", "Rửa và để rau thật ráo; cắt bún thành đoạn ngắn.", 10), S("Cuốn", "Làm ẩm bánh tráng, xếp rau, bún, thịt, tôm và hẹ rồi cuốn chặt tay.", 15), S("Pha tương", "Đun tương đen với chút nước, thêm bơ đậu phộng, tỏi và ớt; rắc đậu phộng rang.", 5)]),

        R("bo-bia-man", "Bò bía mặn", "mon-cuon-va-goi", "Bò bía cuốn lạp xưởng, trứng, củ sắn xào, tôm khô và xà lách, chấm tương đậu.", 35, 25, 4, RecipeDifficulty.Medium, N(345, 15, 42, 14, 5, 720),
            [I("Bánh tráng", 16, "miếng"), I("Lạp xưởng", 200, "g"), I("Củ sắn", 500, "g"), I("Cà rốt", 150, "g"), I("Trứng gà", 3, "quả"), I("Tôm khô", 60, "g"), I("Xà lách", 150, "g"), I("Tương đen", 120, "ml")],
            [S("Chuẩn bị nhân", "Thái sợi củ sắn, cà rốt; chiên trứng mỏng rồi thái sợi, áp chảo lạp xưởng.", 15), S("Xào củ sắn", "Xào củ sắn, cà rốt và tôm khô đến chín nhưng còn giòn.", 10), S("Cuốn bò bía", "Làm ẩm bánh tráng, xếp xà lách, nhân xào, trứng và lạp xưởng rồi cuốn gọn.", 15), S("Pha tương", "Nấu tương đen với bơ đậu phộng và chút nước; thêm đồ chua, ớt nếu thích.", 5)]),

        R("goi-ga-bap-cai", "Gỏi gà bắp cải", "mon-cuon-va-goi", "Gà xé trộn bắp cải giòn, cà rốt, hành tây, rau răm và nước mắm chua ngọt.", 30, 25, 4, RecipeDifficulty.Easy, N(295, 29, 19, 12, 6, 720),
            [I("Ức gà", 600, "g"), I("Bắp cải", 500, "g"), I("Cà rốt", 150, "g"), I("Hành tây", 1, "củ"), I("Rau răm", 50, "g"), I("Đậu phộng rang", 70, "g"), I("Chanh", 3, "quả"), I("Nước mắm", 3, "muỗng canh")],
            [S("Luộc gà", "Luộc gà với gừng và hành đến vừa chín, để nguội rồi xé sợi.", 20), S("Sơ chế rau", "Thái mỏng bắp cải, cà rốt và hành tây; ngâm hành trong nước đá cho bớt hăng.", 10), S("Pha nước trộn", "Khuấy nước mắm, nước cốt chanh, đường, tỏi và ớt cho tan.", 5), S("Trộn gỏi", "Trộn gà và rau với nước mắm, thêm rau răm; rắc đậu phộng và hành phi trước khi dùng.", 5)]),

        R("nom-hoa-chuoi", "Nộm hoa chuối", "mon-cuon-va-goi", "Hoa chuối giòn trộn thịt, tôm, rau thơm, đậu phộng và nước mắm chua ngọt.", 35, 15, 4, RecipeDifficulty.Medium, N(285, 20, 22, 14, 8, 690),
            [I("Hoa chuối", 500, "g"), I("Thịt nạc vai", 250, "g"), I("Tôm", 250, "g"), I("Cà rốt", 120, "g"), I("Rau thơm", 100, "g"), I("Đậu phộng rang", 70, "g"), I("Chanh", 3, "quả"), I("Nước mắm", 3, "muỗng canh")],
            [S("Ngâm hoa chuối", "Bào mỏng hoa chuối, ngâm ngay trong nước chanh loãng để không thâm rồi vắt ráo.", 20), S("Luộc thịt tôm", "Luộc thịt và tôm vừa chín; thái thịt mỏng, bóc tôm.", 15), S("Pha nước trộn", "Khuấy nước mắm, chanh, đường, tỏi và ớt cho vị chua ngọt cân bằng.", 5), S("Trộn nộm", "Trộn hoa chuối, cà rốt, thịt, tôm và rau thơm; rắc đậu phộng, hành phi.", 5)]),

        R("goi-ngo-sen-tom-thit", "Gỏi ngó sen tôm thịt", "mon-cuon-va-goi", "Ngó sen giòn mát trộn tôm, thịt, rau răm và nước mắm chua ngọt.", 35, 20, 4, RecipeDifficulty.Medium, N(300, 24, 24, 12, 6, 710),
            [I("Ngó sen", 500, "g"), I("Tôm", 300, "g"), I("Thịt ba chỉ", 250, "g"), I("Cà rốt", 120, "g"), I("Dưa leo", 1, "quả"), I("Rau răm", 50, "g"), I("Đậu phộng rang", 70, "g"), I("Nước mắm", 3, "muỗng canh")],
            [S("Sơ chế ngó sen", "Rửa ngó sen, chẻ vừa ăn và ngâm nước giấm đường lạnh cho trắng giòn.", 20), S("Luộc tôm thịt", "Luộc tôm và thịt riêng đến vừa chín; bóc tôm, thái thịt mỏng.", 15), S("Pha nước trộn", "Hòa nước mắm, chanh, đường, tỏi và ớt đến khi tan.", 5), S("Trộn gỏi", "Vắt ráo ngó sen, trộn với tôm thịt, cà rốt, dưa leo và rau răm; rắc đậu phộng.", 5)]),

        R("banh-xeo-mien-tay", "Bánh xèo miền Tây", "banh-viet", "Bánh xèo lớn, vỏ mỏng giòn từ bột gạo và nghệ, nhân tôm thịt, giá và đậu xanh.", 40, 35, 4, RecipeDifficulty.Hard, N(520, 22, 56, 24, 6, 760),
            [I("Bột gạo", 400, "g"), I("Nước cốt dừa", 300, "ml"), I("Bột nghệ", 1, "thìa cà phê"), I("Tôm", 350, "g"), I("Thịt ba chỉ", 300, "g"), I("Giá đỗ", 500, "g"), I("Đậu xanh hấp", 150, "g"), I("Rau sống", 500, "g")],
            [S("Pha bột", "Khuấy bột gạo, nước cốt dừa, nước, nghệ, muối và hành lá; để bột nghỉ 30 phút.", 30), S("Chuẩn bị nhân", "Xào tôm và thịt ba chỉ vừa chín, nêm nhẹ.", 8), S("Đổ bánh", "Làm nóng chảo, cho tôm thịt rồi tráng lớp bột thật mỏng; thêm giá và đậu xanh, đậy nắp.", 6), S("Làm giòn", "Mở nắp, thêm ít dầu quanh viền, chiên đến giòn rồi gập đôi; dùng với rau và nước mắm.", 4)]),

        R("banh-khot-vung-tau", "Bánh khọt Vũng Tàu", "banh-viet", "Bánh khọt nhỏ giòn đáy, mềm trong, mỗi chiếc có tôm, mỡ hành và tôm chấy.", 35, 30, 4, RecipeDifficulty.Hard, N(470, 21, 51, 21, 4, 740),
            [I("Bột gạo", 350, "g"), I("Nước cốt dừa", 200, "ml"), I("Bột nghệ", 0.5m, "thìa cà phê"), I("Tôm nhỏ", 400, "g"), I("Tôm khô", 70, "g"), I("Hành lá", 60, "g"), I("Rau sống", 400, "g"), I("Dầu ăn", 100, "ml")],
            [S("Pha bột", "Trộn bột gạo, nước cốt dừa, nước, nghệ và muối; để nghỉ 30 phút.", 30), S("Làm tôm chấy", "Ngâm tôm khô, giã bông rồi rang tơi với chút dầu.", 10), S("Đổ bánh", "Làm nóng khuôn, cho dầu và bột vào từng ô, đặt một con tôm lên mặt rồi đậy nắp.", 6), S("Hoàn thiện", "Chiên đến viền giòn, lấy bánh ra, thêm mỡ hành và tôm chấy; dùng với rau và nước mắm.", 4)]),

        R("banh-cuon-ha-noi", "Bánh cuốn Hà Nội", "banh-viet", "Lớp bánh gạo hấp mỏng cuốn nhân thịt nấm, phủ hành phi và ăn cùng chả quế.", 45, 35, 4, RecipeDifficulty.Expert, N(455, 22, 61, 14, 4, 880),
            [I("Bột gạo", 350, "g"), I("Bột năng", 80, "g"), I("Thịt heo xay", 350, "g"), I("Nấm mèo", 25, "g"), I("Hành tím", 8, "củ"), I("Chả quế", 300, "g"), I("Nước mắm", 4, "muỗng canh"), I("Dầu ăn", 60, "ml")],
            [S("Pha bột", "Khuấy bột gạo, bột năng, nước, muối và dầu; để bột nghỉ ít nhất 30 phút.", 30), S("Xào nhân", "Xào thịt với nấm mèo và hành tím đến tơi, nêm nước mắm và tiêu.", 10), S("Tráng bánh", "Tráng lớp bột rất mỏng trên vải hấp căng, đậy 40–50 giây rồi lấy bánh ra.", 15), S("Cuốn và dọn", "Cho nhân vào, cuốn nhẹ; phủ hành phi, dùng với chả quế và nước mắm pha ấm.", 10)]),

        R("banh-beo-chen", "Bánh bèo chén", "banh-viet", "Bánh bèo Huế mềm mượt trong chén nhỏ, phủ tôm chấy, mỡ hành và tóp giòn.", 35, 30, 6, RecipeDifficulty.Hard, N(360, 13, 52, 11, 3, 760),
            [I("Bột gạo", 350, "g"), I("Bột năng", 50, "g"), I("Tôm", 300, "g"), I("Da heo chiên", 100, "g"), I("Hành lá", 60, "g"), I("Nước mắm", 4, "muỗng canh"), I("Đường", 3, "muỗng canh"), I("Chanh", 1, "quả")],
            [S("Pha bột", "Khuấy bột gạo, bột năng, nước và muối; để nghỉ 30 phút rồi khuấy lại.", 30), S("Làm tôm chấy", "Hấp tôm, bóc vỏ, giã bông rồi rang khô tơi với chút dầu điều.", 12), S("Hấp bánh", "Làm nóng chén, rót lớp bột mỏng và hấp 5–7 phút đến trong.", 15), S("Hoàn thiện", "Phủ tôm chấy, mỡ hành, da heo giòn; dùng với nước mắm chua ngọt.", 5)]),

        R("banh-bot-loc-hue", "Bánh bột lọc Huế", "banh-viet", "Bánh bột năng trong dai bọc tôm thịt rim đậm vị, ăn với nước mắm ớt.", 45, 35, 4, RecipeDifficulty.Hard, N(405, 18, 58, 11, 2, 820),
            [I("Bột năng", 500, "g"), I("Tôm nhỏ", 350, "g"), I("Thịt ba chỉ", 250, "g"), I("Hành tím", 4, "củ"), I("Dầu điều", 1, "muỗng canh"), I("Nước mắm", 4, "muỗng canh"), I("Đường", 2, "muỗng canh"), I("Hành lá", 50, "g")],
            [S("Rim nhân", "Cắt nhỏ thịt, rim cùng tôm, hành tím, nước mắm, đường và dầu điều đến cạn.", 20), S("Nhồi bột", "Rót nước sôi từ từ vào bột năng, trộn và nhồi đến khối bột dẻo mịn.", 12), S("Tạo bánh", "Chia bột, cán mỏng, đặt tôm thịt vào giữa rồi gấp và miết kín mép.", 15), S("Luộc bánh", "Luộc đến khi bánh nổi và trong, vớt vào nước mát rồi trộn mỡ hành.", 10)]),

        R("banh-it-tran", "Bánh ít trần", "banh-viet", "Bánh nếp dẻo nhân đậu xanh thịt mặn, phủ mỡ hành, tôm chấy và hành phi.", 45, 40, 6, RecipeDifficulty.Hard, N(420, 15, 63, 12, 5, 690),
            [I("Bột nếp", 500, "g"), I("Đậu xanh cà", 250, "g"), I("Thịt heo xay", 200, "g"), I("Tôm", 200, "g"), I("Hành tím", 6, "củ"), I("Hành lá", 60, "g"), I("Nước mắm", 3, "muỗng canh"), I("Dầu ăn", 3, "muỗng canh")],
            [S("Làm nhân", "Hấp chín đậu xanh, tán nhuyễn; xào với thịt, tôm băm và hành tím rồi vo viên.", 25), S("Nhồi bột", "Nhồi bột nếp với nước ấm và chút muối đến dẻo mịn, để nghỉ 15 phút.", 20), S("Tạo bánh", "Bọc bột quanh viên nhân, vo tròn và đặt lên lá chuối đã quét dầu.", 15), S("Hấp bánh", "Hấp 15–18 phút đến khi vỏ trong dẻo, phủ mỡ hành, tôm chấy và hành phi.", 18)]),

        R("bun-cha-ha-noi", "Bún chả Hà Nội", "mon-nuong", "Chả viên và ba chỉ nướng than thơm, ngâm trong nước mắm ấm cùng đu đủ xanh.", 40, 30, 4, RecipeDifficulty.Medium, N(590, 34, 63, 23, 5, 1120),
            [I("Thịt ba chỉ", 500, "g"), I("Thịt vai xay", 400, "g"), I("Bún tươi", 800, "g"), I("Đu đủ xanh", 200, "g"), I("Cà rốt", 100, "g"), I("Nước mắm", 6, "muỗng canh"), I("Đường", 5, "muỗng canh"), I("Rau sống", 400, "g")],
            [S("Ướp thịt", "Ướp ba chỉ thái mỏng và thịt xay riêng với nước mắm, đường, hành tím, tỏi và tiêu.", 30), S("Làm đồ chua", "Bóp đu đủ và cà rốt với muối, rửa rồi ngâm giấm đường.", 20), S("Nướng chả", "Vo thịt xay thành viên dẹt, nướng cùng ba chỉ trên than đến xém thơm và chín.", 15), S("Pha nước chấm", "Pha nước mắm ấm chua ngọt, thêm đồ chua và chả nướng; dùng với bún và rau.", 10)]),

        R("thit-nuong-sa", "Thịt nướng sả", "mon-nuong", "Thịt vai ướp sả, tỏi và nước mắm, nướng xém cạnh nhưng vẫn mềm mọng.", 30, 18, 4, RecipeDifficulty.Easy, N(430, 31, 20, 25, 2, 830),
            [I("Thịt vai heo", 800, "g"), I("Sả", 5, "cây"), I("Tỏi", 5, "tép"), I("Hành tím", 4, "củ"), I("Nước mắm", 3, "muỗng canh"), I("Mật ong", 2, "muỗng canh"), I("Dầu ăn", 2, "muỗng canh"), I("Bún và rau", 800, "g")],
            [S("Thái và ướp", "Thái thịt mỏng, ướp sả băm, tỏi, hành, nước mắm, mật ong và tiêu.", 30), S("Xiên thịt", "Xiên thịt lỏng tay hoặc trải thành một lớp trên vỉ để chín đều.", 8), S("Nướng", "Nướng than lửa vừa, trở mặt và quét dầu đến khi thịt xém cạnh, chín mọng.", 12), S("Trình bày", "Dùng với bún, đồ chua, rau thơm, mỡ hành và nước mắm chua ngọt.", 5)]),

        R("ga-nuong-muoi-ot", "Gà nướng muối ớt", "mon-nuong", "Gà nguyên con ướp muối ớt, sả và mật ong, nướng da giòn, thịt mọng.", 35, 55, 6, RecipeDifficulty.Medium, N(420, 42, 10, 24, 1, 860),
            [I("Gà ta", 1.6m, "kg"), I("Ớt sừng", 5, "quả"), I("Sả", 4, "cây"), I("Tỏi", 6, "tép"), I("Muối hột", 1, "muỗng canh"), I("Mật ong", 2, "muỗng canh"), I("Dầu điều", 1, "muỗng canh"), I("Chanh", 2, "quả")],
            [S("Pha sốt ướp", "Giã ớt, sả, tỏi và muối; trộn với mật ong, dầu điều và chút nước mắm.", 10), S("Ướp gà", "Mổ phanh gà, thấm khô rồi xoa sốt cả ngoài da lẫn mặt trong; ướp ít nhất 2 giờ.", 120), S("Nướng gà", "Nướng 180°C hoặc trên than lửa vừa, trở đều và quét sốt đến khi da vàng giòn.", 50), S("Nghỉ thịt", "Để gà nghỉ 8 phút trước khi chặt, dùng với dưa leo, rau thơm và muối tiêu chanh.", 8)]),

        R("ca-loc-nuong-trui", "Cá lóc nướng trui", "mon-nuong", "Cá lóc nướng nguyên con bằng rơm hoặc than, thịt ngọt cuốn bánh tráng và rau đồng.", 25, 35, 4, RecipeDifficulty.Hard, N(350, 41, 16, 13, 4, 620),
            [I("Cá lóc", 1.2m, "kg"), I("Bánh tráng", 20, "miếng"), I("Bún tươi", 500, "g"), I("Dứa", 250, "g"), I("Dưa leo", 2, "quả"), I("Rau sống", 500, "g"), I("Me chín", 80, "g"), I("Nước mắm", 4, "muỗng canh")],
            [S("Chuẩn bị cá", "Giữ nguyên vảy, rửa sạch nhớt và xiên cá từ miệng đến đuôi để giữ thẳng.", 15), S("Nướng cá", "Phủ rơm hoặc nướng than đến khi lớp vảy cháy đen, thịt bên trong chín thơm.", 30), S("Làm nước chấm", "Dầm me với nước nóng, lọc rồi pha nước mắm, đường, tỏi và ớt.", 8), S("Trình bày", "Gỡ lớp da cháy, lấy thịt cá cuốn bánh tráng với bún, dứa, dưa leo và rau.", 8)]),

        R("dau-hu-kho-nam", "Đậu hũ kho nấm", "mon-chay", "Đậu hũ vàng kho cùng nấm hương và nấm rơm trong nước tương mặn ngọt vừa vị.", 20, 25, 4, RecipeDifficulty.Easy, N(280, 17, 20, 17, 6, 680),
            [I("Đậu hũ", 600, "g"), I("Nấm hương tươi", 200, "g"), I("Nấm rơm", 250, "g"), I("Nước dừa", 300, "ml"), I("Nước tương", 3, "muỗng canh"), I("Đường", 1, "muỗng canh"), I("Gừng", 20, "g"), I("Hành lá", 40, "g")],
            [S("Sơ chế", "Cắt đậu hũ thành khối, áp chảo vàng; làm sạch và cắt nấm vừa ăn.", 15), S("Xào nấm", "Phi gừng, xào nấm lửa lớn đến thơm và vừa ra nước.", 5), S("Kho", "Thêm đậu hũ, nước dừa, nước tương và đường; kho nhỏ lửa đến khi nước sánh.", 18), S("Hoàn thiện", "Nêm lại, thêm tiêu và hành lá hoặc hành boa-rô.", 2)]),

        R("bun-hue-chay", "Bún Huế chay", "mon-chay", "Nước dùng rau củ thơm sả và sa tế, ăn cùng đậu hũ, nấm, tàu hũ ky và bún sợi lớn.", 35, 65, 6, RecipeDifficulty.Medium, N(380, 16, 56, 12, 8, 890),
            [I("Bún sợi lớn", 1, "kg"), I("Đậu hũ", 500, "g"), I("Nấm hương", 200, "g"), I("Tàu hũ ky", 150, "g"), I("Dứa", 250, "g"), I("Sả", 6, "cây"), I("Củ cải trắng", 400, "g"), I("Rau sống", 400, "g")],
            [S("Nấu nước dùng", "Hầm củ cải, bắp cải, cà rốt và sả với 3 lít nước khoảng 45 phút rồi lọc.", 45), S("Chuẩn bị đồ ăn kèm", "Chiên đậu hũ, ngâm nấm và tàu hũ ky; xào nấm với sả băm.", 15), S("Nêm nước", "Cho dứa, nấm và tàu hũ ky vào nước dùng; nêm nước tương, muối và sa tế chay.", 15), S("Trình bày", "Cho bún và đậu hũ vào tô, chan nước dùng, dùng với rau sống và chanh.", 5)]),

        R("ca-ri-chay", "Cà ri chay", "mon-chay", "Cà ri nước cốt dừa béo dịu với đậu hũ, khoai lang, khoai môn, cà rốt và nấm.", 30, 40, 6, RecipeDifficulty.Medium, N(410, 11, 50, 20, 9, 640),
            [I("Đậu hũ", 500, "g"), I("Khoai lang", 400, "g"), I("Khoai môn", 300, "g"), I("Cà rốt", 250, "g"), I("Nấm", 300, "g"), I("Nước cốt dừa", 500, "ml"), I("Bột cà ri", 2, "muỗng canh"), I("Sả", 4, "cây")],
            [S("Sơ chế", "Cắt rau củ và đậu hũ miếng lớn; chiên sơ khoai và đậu để không vỡ khi nấu.", 20), S("Xào gia vị", "Phi sả, thêm bột cà ri và một phần nước cốt dừa, khuấy đến thơm.", 5), S("Nấu cà ri", "Thêm rau củ cứng và nước, nấu gần mềm rồi cho nấm, đậu hũ và nước cốt dừa còn lại.", 30), S("Hoàn thiện", "Nêm muối, đường và nước tương; dùng nóng với bánh mì.", 5)]),

        R("cha-gio", "Chả giò", "mon-an-vat", "Chả giò vỏ mỏng giòn, nhân thịt tôm, khoai môn, miến và nấm mèo đậm vị.", 45, 25, 6, RecipeDifficulty.Medium, N(390, 19, 35, 20, 4, 790),
            [I("Bánh tráng chả giò", 30, "miếng"), I("Thịt heo xay", 400, "g"), I("Tôm", 250, "g"), I("Khoai môn", 250, "g"), I("Miến", 60, "g"), I("Nấm mèo", 25, "g"), I("Cà rốt", 120, "g"), I("Trứng gà", 1, "quả")],
            [S("Trộn nhân", "Băm tôm, thái nhỏ miến và nấm; trộn cùng thịt, khoai môn, cà rốt, trứng và gia vị.", 20), S("Cuốn", "Đặt lượng nhân vừa phải lên bánh tráng, gấp hai bên và cuốn chắc nhưng không quá chặt.", 20), S("Chiên lần một", "Chiên ngập dầu ở 150°C đến chả giò chín và vàng nhạt, vớt để nguội.", 12), S("Chiên giòn", "Chiên lần hai ở 180°C đến vàng giòn; dùng với rau sống và nước mắm chua ngọt.", 4)]),

        R("che-ba-mau", "Chè ba màu", "mon-trang-mieng", "Chè lạnh ba tầng đậu đỏ, đậu xanh và thạch lá dứa, chan nước cốt dừa.", 30, 60, 6, RecipeDifficulty.Medium, N(385, 9, 65, 11, 9, 120),
            [I("Đậu đỏ", 200, "g"), I("Đậu xanh cà", 200, "g"), I("Bột rau câu", 10, "g"), I("Nước lá dứa", 300, "ml"), I("Nước cốt dừa", 400, "ml"), I("Đường", 250, "g"), I("Bột năng", 1, "muỗng canh"), I("Đá bào", 600, "g")],
            [S("Nấu đậu đỏ", "Ngâm đậu đỏ 6 giờ, nấu mềm rồi thêm đường vừa ngọt.", 45), S("Nấu đậu xanh", "Hấp đậu xanh chín, tán nhuyễn với đường và chút nước cốt dừa.", 25), S("Làm thạch", "Nấu bột rau câu với nước lá dứa và đường, đổ khuôn rồi cắt hạt lựu.", 20), S("Hoàn thiện", "Xếp thạch, đậu đỏ, đậu xanh vào ly; thêm đá bào và nước cốt dừa đã nấu sánh.", 5)]),

        R("chuoi-nep-nuong", "Chuối nếp nướng", "mon-trang-mieng", "Chuối sứ bọc nếp dẻo nướng thơm, chan nước cốt dừa và rắc đậu phộng mè.", 35, 40, 6, RecipeDifficulty.Medium, N(405, 7, 68, 13, 5, 150),
            [I("Chuối sứ chín", 8, "quả"), I("Gạo nếp", 500, "g"), I("Nước cốt dừa", 500, "ml"), I("Lá chuối", 8, "miếng"), I("Đậu phộng rang", 80, "g"), I("Mè rang", 30, "g"), I("Đường", 80, "g"), I("Bột năng", 1, "muỗng canh")],
            [S("Đồ nếp", "Ngâm nếp, hấp gần chín rồi trộn với một phần nước cốt dừa, đường và muối.", 30), S("Bọc chuối", "Ép nếp thành lớp mỏng, bọc kín từng quả chuối rồi gói lá chuối.", 15), S("Nướng", "Nướng trên than hoặc ở 190°C, trở đều đến khi lá cháy thơm và nếp vàng giòn nhẹ.", 25), S("Làm sốt", "Nấu nước cốt dừa còn lại với đường, muối và bột năng; chan lên chuối, rắc đậu phộng mè.", 8)]),
    ];

    private static readonly Lazy<IReadOnlyList<SeedRecipe>> AllRecipes = new(() =>
        CoreRecipes.Concat(AdditionalRecipes).Select(EnsureCompleteComposition).ToArray());

    public static IReadOnlyList<SeedRecipe> Recipes => AllRecipes.Value;

    private static SeedRecipe EnsureCompleteComposition(SeedRecipe recipe)
    {
        var ingredients = recipe.Ingredients.ToList();
        foreach (var ingredient in SupplementaryIngredients(recipe.CategorySlug))
        {
            if (ingredients.Count >= 10)
            {
                break;
            }

            if (!ingredients.Any(existing => string.Equals(existing.Name, ingredient.Name, StringComparison.OrdinalIgnoreCase)))
            {
                ingredients.Add(ingredient);
            }
        }

        if (ingredients.Count < 10 &&
            !ingredients.Any(existing => string.Equals(existing.Name, "Nước lọc", StringComparison.OrdinalIgnoreCase)))
        {
            ingredients.Add(I("Nước lọc", 500, "ml", "điều chỉnh theo cách nấu"));
        }

        var steps = recipe.Steps.ToList();
        if (steps.Count < 5)
        {
            steps.Add(S(
                "Nêm lại và hoàn thiện",
                "Kiểm tra độ chín, nêm lại cho hài hòa rồi trình bày món ăn khi còn ở trạng thái ngon nhất.",
                5));
        }

        if (ingredients.Count < 10 || steps.Count < 5)
        {
            throw new InvalidOperationException($"Seed recipe '{recipe.Slug}' does not have a complete composition.");
        }

        return recipe with { Ingredients = ingredients, Steps = steps };
    }

    private static IReadOnlyList<SeedIngredient> SupplementaryIngredients(string categorySlug) => categorySlug switch
    {
        "mon-nuoc" => [I("Muối", 1, "thìa cà phê"), I("Đường phèn", 20, "g"), I("Hành tím", 4, "củ"), I("Tiêu", 0.5m, "thìa cà phê"), I("Chanh", 2, "quả"), I("Ớt tươi", 2, "quả"), I("Tỏi", 3, "tép"), I("Giá đỗ", 200, "g"), I("Rau thơm", 100, "g")],
        "com-va-xoi" => [I("Muối", 1, "thìa cà phê"), I("Dầu ăn", 2, "muỗng canh"), I("Hành phi", 30, "g"), I("Nước mắm", 2, "muỗng canh"), I("Tiêu", 0.5m, "thìa cà phê"), I("Hành tím", 3, "củ"), I("Tỏi", 3, "tép"), I("Mè rang", 20, "g"), I("Rau thơm", 50, "g")],
        "mon-kho-va-rim" => [I("Muối", 0.5m, "thìa cà phê"), I("Dầu ăn", 2, "muỗng canh"), I("Hành lá", 30, "g"), I("Nước màu", 1, "muỗng canh"), I("Tiêu", 0.5m, "thìa cà phê"), I("Tỏi", 4, "tép"), I("Gừng", 20, "g"), I("Sả", 2, "cây"), I("Ớt tươi", 2, "quả")],
        "mon-xao" => [I("Dầu ăn", 2, "muỗng canh"), I("Tỏi", 4, "tép"), I("Nước mắm", 1, "muỗng canh"), I("Hành lá", 30, "g"), I("Tiêu", 0.5m, "thìa cà phê"), I("Đường", 0.5m, "thìa cà phê"), I("Hạt nêm", 1, "thìa cà phê"), I("Ớt tươi", 1, "quả"), I("Gừng", 15, "g")],
        "canh-va-sup" => [I("Muối", 1, "thìa cà phê"), I("Nước mắm", 1, "muỗng canh"), I("Hành tím", 2, "củ"), I("Hành lá", 30, "g"), I("Tiêu", 0.5m, "thìa cà phê"), I("Rau mùi", 30, "g"), I("Gừng", 15, "g"), I("Tỏi", 2, "tép"), I("Ớt tươi", 1, "quả")],
        "mon-cuon-va-goi" => [I("Đường", 2, "muỗng canh"), I("Tỏi", 3, "tép"), I("Ớt tươi", 2, "quả"), I("Hành phi", 30, "g"), I("Giấm gạo", 2, "muỗng canh"), I("Nước mắm", 3, "muỗng canh"), I("Chanh", 2, "quả"), I("Rau thơm", 100, "g"), I("Đậu phộng", 60, "g")],
        "banh-viet" => [I("Muối", 1, "thìa cà phê"), I("Đường", 1, "muỗng canh"), I("Tiêu", 0.5m, "thìa cà phê"), I("Hành phi", 30, "g"), I("Lá chuối", 6, "miếng"), I("Nước lọc", 500, "ml"), I("Hành lá", 40, "g"), I("Nước mắm", 2, "muỗng canh"), I("Dầu ăn", 2, "muỗng canh")],
        "mon-nuong" => [I("Muối", 1, "thìa cà phê"), I("Tiêu", 0.5m, "thìa cà phê"), I("Nước mắm", 2, "muỗng canh"), I("Rau thơm", 100, "g"), I("Ớt tươi", 2, "quả"), I("Tỏi", 4, "tép"), I("Sả", 3, "cây"), I("Mật ong", 1, "muỗng canh"), I("Dầu ăn", 2, "muỗng canh")],
        "mon-chay" => [I("Muối", 1, "thìa cà phê"), I("Dầu thực vật", 2, "muỗng canh"), I("Tiêu", 0.5m, "thìa cà phê"), I("Hành boa-rô", 40, "g"), I("Rau mùi", 30, "g"), I("Nước tương", 2, "muỗng canh"), I("Đường", 1, "thìa cà phê"), I("Gừng", 15, "g"), I("Mè rang", 20, "g")],
        "mon-an-vat" => [I("Muối", 1, "thìa cà phê"), I("Đường", 1, "muỗng canh"), I("Tỏi", 3, "tép"), I("Ớt tươi", 2, "quả"), I("Rau răm", 50, "g"), I("Nước mắm", 2, "muỗng canh"), I("Chanh", 2, "quả"), I("Hành phi", 30, "g"), I("Đậu phộng", 50, "g")],
        "mon-trang-mieng" => [I("Muối", 0.25m, "thìa cà phê"), I("Lá dứa", 3, "lá"), I("Vani", 1, "ống"), I("Dừa nạo", 80, "g"), I("Mè rang", 20, "g"), I("Nước lọc", 500, "ml"), I("Nước cốt dừa", 200, "ml"), I("Đường phèn", 80, "g"), I("Đậu phộng", 50, "g")],
        "mon-ham" => [I("Muối", 1, "thìa cà phê"), I("Hành tím", 4, "củ"), I("Tiêu", 0.5m, "thìa cà phê"), I("Rau mùi", 40, "g"), I("Nước mắm", 2, "muỗng canh"), I("Gừng", 30, "g"), I("Tỏi", 4, "tép"), I("Cà rốt", 1, "củ"), I("Hành lá", 40, "g")],
        "hai-san" => [I("Muối", 1, "thìa cà phê"), I("Gừng", 30, "g"), I("Tỏi", 4, "tép"), I("Hành lá", 30, "g"), I("Tiêu", 0.5m, "thìa cà phê")],
        "dac-san-vung-mien" => [I("Muối", 1, "thìa cà phê"), I("Nước mắm", 2, "muỗng canh"), I("Hành tím", 4, "củ"), I("Tỏi", 4, "tép"), I("Tiêu", 0.5m, "thìa cà phê")],
        "mon-hap" => [I("Muối", 1, "thìa cà phê"), I("Gừng", 30, "g"), I("Hành lá", 40, "g"), I("Tiêu", 0.5m, "thìa cà phê"), I("Nước mắm", 2, "muỗng canh")],
        "mon-lau" => [I("Muối", 1, "thìa cà phê"), I("Nước mắm", 3, "muỗng canh"), I("Ớt tươi", 3, "quả"), I("Bún tươi", 1, "kg"), I("Rau ăn lẩu", 600, "g")],
        "mon-chien" => [I("Dầu ăn", 500, "ml"), I("Muối", 1, "thìa cà phê"), I("Tiêu", 0.5m, "thìa cà phê"), I("Tỏi", 3, "tép"), I("Rau thơm", 80, "g")],
        "mon-luoc" => [I("Muối", 1, "thìa cà phê"), I("Gừng", 30, "g"), I("Hành tím", 3, "củ"), I("Rau thơm", 80, "g"), I("Nước mắm", 3, "muỗng canh")],
        "do-chua-va-muoi" => [I("Muối hột", 40, "g"), I("Đường", 60, "g"), I("Giấm gạo", 100, "ml"), I("Tỏi", 4, "tép"), I("Ớt tươi", 3, "quả")],
        "do-uong-viet" => [I("Đường phèn", 80, "g"), I("Nước lọc", 1, "lít"), I("Đá viên", 500, "g"), I("Lá dứa", 2, "lá"), I("Muối", 0.25m, "thìa cà phê")],
        _ => throw new InvalidOperationException($"No ingredient supplements configured for category '{categorySlug}'."),
    };

    private static SeedRecipe R(string slug, string title, string categorySlug, string description, int prepTime,
        int cookTime, int servings, RecipeDifficulty difficulty, SeedNutrition nutrition,
        IReadOnlyList<SeedIngredient> ingredients, IReadOnlyList<SeedStep> steps) =>
        new(slug, title, categorySlug, description, prepTime, cookTime, servings, difficulty, nutrition, ingredients, steps);

    private static SeedIngredient I(string name, decimal quantity, string unit, string? notes = null) =>
        new(name, quantity, unit, notes);

    private static SeedStep S(string title, string description, int? timerMinutes = null) =>
        new(title, description, timerMinutes);

    private static SeedNutrition N(decimal calories, decimal protein, decimal carbohydrates, decimal fat,
        decimal fiber, decimal sodium) => new(calories, protein, carbohydrates, fat, fiber, sodium);
}

internal sealed record SeedAuthor(string Email, string DisplayName, string Bio);

internal sealed record SeedCategory(string Slug, string Name, string Description);

internal sealed record SeedRecipe(
    string Slug,
    string Title,
    string CategorySlug,
    string Description,
    int PrepTime,
    int CookTime,
    int Servings,
    RecipeDifficulty Difficulty,
    SeedNutrition Nutrition,
    IReadOnlyList<SeedIngredient> Ingredients,
    IReadOnlyList<SeedStep> Steps);

internal sealed record SeedIngredient(string Name, decimal Quantity, string Unit, string? Notes);

internal sealed record SeedStep(string Title, string Description, int? TimerMinutes);

internal sealed record SeedNutrition(
    decimal Calories,
    decimal Protein,
    decimal Carbohydrates,
    decimal Fat,
    decimal Fiber,
    decimal Sodium);
