// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the vietnamese seed data.</summary>
internal static partial class VietnameseSeedData
{
    private static readonly Lazy<IReadOnlyList<SeedRecipe>> AllRecipes = new(BuildRecipes);

    /// <summary>Gets the authors.</summary>
    public static IReadOnlyList<SeedAuthor> Authors { get; } =
    [
        new("phase8-author-01@example.test", "Nguyễn Minh Anh", "Yêu bếp Việt và chuyên ghi chép các món ăn gia đình miền Bắc."),
        new("phase8-author-02@example.test", "Trần Hoàng Nam", "Chia sẻ món ngon miền Trung với cách nấu rõ ràng, dễ làm tại nhà."),
        new("phase8-author-03@example.test", "Lê Thảo Vy", "Đam mê ẩm thực Nam Bộ, các món bánh và món ăn đường phố Việt Nam."),
        new("phase8-author-04@example.test", "Phạm Gia Huy", "Thích tìm hiểu kỹ thuật kho, nướng và những món Việt truyền thống."),
        new("phase8-author-05@example.test", "Võ Ngọc Mai", "Ưu tiên món chay, món rau và bữa cơm Việt cân bằng, vừa vị."),
    ];

    /// <summary>Gets the categories.</summary>
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

    /// <summary>Gets the recipes.</summary>
    public static IReadOnlyList<SeedRecipe> Recipes => AllRecipes.Value;

    private static IReadOnlyList<SeedRecipe> CoreRecipes { get; } =
    [
        R(
            "pho-bo-ha-noi",
            "Phở bò Hà Nội",
            "mon-nuoc",
            "Nước dùng trong, thơm quế hồi, ăn cùng bánh phở mềm và thịt bò vừa chín tới.",
            40,
            180,
            4,
            RecipeDifficulty.Hard,
            N(485, 31, 58, 14, 3, 980),
            [I("Xương ống bò", 1.5m, "kg"), I("Bánh phở tươi", 600, "g"), I("Thịt bò thăn", 400, "g"), I("Hành tây", 1, "củ"), I("Gừng", 50, "g"), I("Hoa hồi", 4, "cánh"), I("Quế thanh", 1, "thanh"), I("Nước mắm", 3, "muỗng canh"), I("Hành lá và rau mùi", 80, "g")],
            [S("Sơ chế xương", "Chần xương bò 5 phút, rửa sạch. Nướng hành tây và gừng đến khi xém thơm.", 20, "prep"), S("Nấu nước dùng", "Hầm xương với 4 lít nước, hành, gừng, hồi và quế; hớt bọt thường xuyên để nước trong.", 150, "cook"), S("Nêm nước phở", "Lọc nước dùng, nêm nước mắm và muối; giữ sôi nhẹ.", 10, "cook"), S("Hoàn thiện", "Chần bánh phở, xếp thịt bò thái mỏng, chan nước dùng thật nóng rồi thêm hành và rau mùi.", 10, "cook")],
            @"- **Bí quyết nước dùng trong**: Chần kỹ xương ống và rửa sạch máu tụ trước khi ninh. Hầm nhỏ lửa liu riu từ 3-4 tiếng trở lên và hớt bọt liên tục, tuyệt đối không đậy kín nắp.
- **Hương vị quế hồi**: Nướng hành tây, gừng tươi, hoa hồi và thảo quả cho thật xém vỏ để tạo mùi thơm nồng ấm và màu nước dùng vàng óng tự nhiên.
- **Thưởng thức**: Thịt bò tái chần nhanh qua nước dùng đang sôi sùng sục để giữ trọn vị ngọt mềm; dùng kèm quẩy giòn và giấm tỏi ớt chuẩn vị Bắc."),

        R(
            "bun-bo-hue",
            "Bún bò Huế",
            "mon-nuoc",
            "Nước dùng bò sả cay thơm, kết hợp giò heo, chả Huế và sợi bún tròn đặc trưng.",
            45,
            150,
            6,
            RecipeDifficulty.Hard,
            N(560, 34, 62, 20, 4, 1150),
            [I("Xương bò", 1.2m, "kg"), I("Bắp bò", 600, "g"), I("Giò heo", 600, "g"), I("Bún sợi lớn", 1.2m, "kg"), I("Sả", 8, "cây"), I("Mắm ruốc Huế", 2, "muỗng canh"), I("Dầu điều", 2, "muỗng canh"), I("Chả Huế", 300, "g"), I("Rau sống", 400, "g")],
            [S("Sơ chế thịt", "Chần xương, bắp bò và giò heo rồi rửa sạch. Đập dập sả và bó gọn.", 20, "prep"), S("Hầm nước dùng", "Hầm xương, bắp bò, giò heo và sả; vớt từng loại thịt khi vừa mềm.", 120, "cook"), S("Nêm vị Huế", "Hòa mắm ruốc với nước, gạn phần trong vào nồi; thêm dầu điều, ớt và nước mắm.", 15, "cook"), S("Trình bày", "Cho bún, thịt thái lát, giò và chả vào tô, chan nước dùng rồi dùng với rau sống.", 10, "cook")],
            @"- **Xử lý mắm ruốc**: Hòa tan mắm ruốc Huế vào nước lạnh rồi đun sôi, để lắng kỹ và chỉ gạn lấy phần nước trong để nước dùng dậy mùi thơm đặc trưng mà không gắt cặn.
- **Kỹ thuật hầm thịt**: Hầm xương bò cùng sả đập dập; vớt bắp bò và giò heo ra ngâm ngay vào thau nước đá lạnh khi vừa chín tới để thịt săn chắc, da giòn sần sật.
- **Hương vị xứ Huế**: Dùng dầu màu điều xào thơm với ớt bột và sả băm để tạo váng dầu đỏ cam cay nồng hấp dẫn trên mặt tô bún."),

        R(
            "bun-rieu-cua",
            "Bún riêu cua",
            "mon-nuoc",
            "Bún riêu vị chua dịu từ cà chua, riêu cua đồng béo và đậu hũ chiên vàng.",
            35,
            60,
            4,
            RecipeDifficulty.Medium,
            N(430, 24, 55, 13, 5, 920),
            [I("Cua đồng xay", 500, "g"), I("Bún tươi", 800, "g"), I("Cà chua", 5, "quả"), I("Đậu hũ", 3, "miếng"), I("Trứng gà", 2, "quả"), I("Mắm tôm", 1, "muỗng canh"), I("Giấm bỗng", 3, "muỗng canh"), I("Hành lá và tía tô", 80, "g")],
            [S("Lọc cua", "Hòa cua xay với 1,5 lít nước, bóp kỹ rồi lọc bỏ xác.", 15, "prep"), S("Nấu riêu", "Đun nước cua lửa vừa, gạt riêu nổi sang một bên; trộn một phần riêu với trứng để riêu chắc hơn.", 15, "cook"), S("Nấu nước dùng", "Xào cà chua, cho vào nồi cùng đậu hũ; nêm mắm tôm, giấm bỗng và nước mắm.", 20, "cook"), S("Hoàn thiện", "Cho bún vào tô, xếp riêu và đậu hũ, chan nước dùng rồi thêm hành lá, tía tô.", 5, "cook")],
            @"- **Kỹ thuật nấu riêu**: Khuấy đều nước lọc cua theo một chiều trên lửa vừa; khi mảng riêu bắt đầu kết tủa và nổi lên thì hạ lửa nhỏ nhất, vớt riêu ra chén riêng để không bị vỡ nát.
- **Vị chua thanh**: Dùng giấm bỗng nếp lên men tự nhiên kết hợp cà chua xào dầu điều để tạo vị chua dịu nhẹ và màu sắc tươi tắn.
- **Ăn kèm**: Thưởng thức nóng cùng đậu hũ chiên vàng giòn, chả lụa và đĩa rau sống thái mỏng gồm hoa chuối, tía tô, kinh giới."),

        R(
            "hu-tieu-nam-vang",
            "Hủ tiếu Nam Vang",
            "mon-nuoc",
            "Nước dùng xương trong ngọt, dùng với tôm, thịt heo, trứng cút và hủ tiếu dai.",
            40,
            100,
            6,
            RecipeDifficulty.Hard,
            N(510, 30, 66, 13, 3, 1050),
            [I("Xương heo", 1.2m, "kg"), I("Hủ tiếu khô", 700, "g"), I("Tôm sú", 400, "g"), I("Thịt nạc vai", 400, "g"), I("Thịt heo xay", 250, "g"), I("Trứng cút", 18, "quả"), I("Củ cải trắng", 300, "g"), I("Hẹ và cần tàu", 100, "g")],
            [S("Nấu nước dùng", "Chần xương rồi hầm với củ cải; hớt bọt để nước trong.", 90, "cook"), S("Chuẩn bị phần ăn kèm", "Luộc tôm, thịt heo và trứng cút; xào thịt xay với tỏi và chút nước mắm.", 20, "prep"), S("Trụng hủ tiếu", "Ngâm sợi vừa mềm rồi trụng nhanh trong nước sôi, trộn một ít dầu tỏi.", 5, "cook"), S("Hoàn thiện", "Xếp tôm, thịt, trứng lên hủ tiếu, chan nước dùng và thêm hẹ, cần tàu.", 5, "cook")],
            @"- **Nước dùng ngọt thanh**: Hầm xương ống heo cùng củ cải trắng và mực khô hoặc tôm khô nướng sơ để nước dùng đạt độ ngọt tự nhiên đậm đà.
- **Xử lý sợi hủ tiếu**: Ngâm sợi hủ tiếu dai vào nước mát cho mềm nhẹ rồi trụng nhanh qua nước sôi, trộn ngay một muỗng dầu tỏi phi để sợi bóng và không dính bết.
- **Phần thịt xào**: Xào thịt nạc xay trên lửa lớn với tỏi băm và hạt tiêu xay cho thật tơi ráo, làm tăng hương vị thơm béo khi dọn kèm."),

        R(
            "mi-quang-ga",
            "Mì Quảng gà",
            "mon-nuoc",
            "Sợi mì vàng ăn cùng gà thấm nghệ, ít nước nhân đậm vị, đậu phộng và bánh tráng mè.",
            30,
            50,
            4,
            RecipeDifficulty.Medium,
            N(545, 29, 61, 22, 5, 890),
            [I("Mì Quảng", 800, "g"), I("Gà ta", 800, "g"), I("Nghệ tươi", 30, "g"), I("Nén", 20, "g"), I("Đậu phộng rang", 80, "g"), I("Bánh tráng mè", 4, "miếng"), I("Dầu điều", 1, "muỗng canh"), I("Rau sống", 300, "g")],
            [S("Ướp gà", "Chặt gà miếng vừa, ướp nghệ, nén giã, nước mắm, tiêu và dầu điều.", 20, "prep"), S("Nấu nhân", "Xào gà săn, thêm nước vừa ngập và om đến khi gà mềm, nước còn sánh.", 35, "cook"), S("Chuẩn bị mì", "Chần nhanh mì Quảng, để ráo; rửa sạch rau sống.", 5, "prep"), S("Trình bày", "Cho rau và mì vào tô, thêm gà cùng một ít nước nhân, rắc đậu phộng và bánh tráng bẻ.", 5, "cook")],
            @"- **Ướp gà thấm vị**: Chặt gà ta miếng vừa ăn, ướp kỹ cùng củ nén đập dập, nghệ tươi giã nhuyễn và nước mắm nguyên chất ít nhất 30 phút.
- **Độ sánh của nước nhưn**: Nấu gà với lượng nước xâm xấp, kho lửa vừa đến khi thịt gà săn mềm và nước nhưn sánh vàng đậm đà, không nấu ngập như nước lèo phở.
- **Thưởng thức**: Dọn mì với rau sống Trà Quế, rắc đậu phộng rang giòn và bẻ bánh tráng mè nướng giòn rụm ăn kèm."),

        R(
            "bun-ca-nha-trang",
            "Bún cá Nha Trang",
            "mon-nuoc",
            "Nước dùng cá thanh ngọt với chả cá dai, cà chua và dứa tạo vị dịu nhẹ.",
            35,
            70,
            4,
            RecipeDifficulty.Medium,
            N(390, 27, 52, 9, 4, 840),
            [I("Xương cá thu", 800, "g"), I("Chả cá Nha Trang", 500, "g"), I("Bún tươi", 800, "g"), I("Cà chua", 3, "quả"), I("Dứa", 300, "g"), I("Hành tím", 4, "củ"), I("Hành lá", 50, "g"), I("Rau sống", 250, "g")],
            [S("Nấu nước cá", "Chần xương cá, nấu với hành tím nướng rồi lọc kỹ lấy nước trong.", 45, "cook"), S("Hoàn thiện nước dùng", "Xào cà chua, dứa rồi cho vào nước cá; nêm nước mắm và muối.", 15, "cook"), S("Chuẩn bị chả cá", "Chiên hoặc hấp chả cá, cắt lát vừa ăn.", 10, "prep"), S("Trình bày", "Cho bún và chả cá vào tô, chan nước dùng, thêm hành lá và dùng với rau sống.", 5, "cook")],
            @"- **Nước dùng trong ngọt**: Ninh xương cá thu hoặc cá cờ đã rửa sạch khử tanh, kết hợp dứa (thơm) và cà chua xào sơ để nước dùng thanh dịu, không bị tanh.
- **Chả cá dai ngon**: Dùng chả cá nhuyễn quết kỹ, hấp chín hoặc chiên vàng đều hai mặt rồi thái lát xéo vừa ăn.
- **Ăn kèm chuẩn vị**: Dùng kèm chén mắm ớt hiểm cay xé lưỡi, chanh tươi và đĩa rau sống thái nhỏ."),

        R(
            "banh-canh-cua",
            "Bánh canh cua",
            "mon-nuoc",
            "Sợi bánh canh bột năng mềm dai trong nước cua sánh, có tôm và trứng cút.",
            35,
            55,
            4,
            RecipeDifficulty.Medium,
            N(520, 30, 67, 15, 3, 1080),
            [I("Bánh canh bột năng", 800, "g"), I("Cua biển", 2, "con", "khoảng 700 g"), I("Tôm sú", 300, "g"), I("Trứng cút", 12, "quả"), I("Xương heo", 600, "g"), I("Bột năng", 3, "muỗng canh"), I("Dầu điều", 1, "muỗng canh"), I("Hành lá và ngò", 60, "g")],
            [S("Nấu nước dùng", "Hầm xương heo lấy nước ngọt, lọc bỏ xương và cặn.", 40, "cook"), S("Chuẩn bị hải sản", "Luộc cua và tôm vừa chín; gỡ thịt cua, bóc vỏ tôm.", 15, "prep"), S("Nấu nước cua", "Xào thịt cua với dầu điều, cho vào nước dùng; thêm bột năng pha loãng đến độ sánh vừa.", 10, "cook"), S("Hoàn thiện", "Luộc bánh canh, cho vào tô cùng cua, tôm, trứng cút rồi chan nước dùng.", 5, "cook")],
            @"- **Độ sánh nước dùng**: Nấu nước dùng từ xương heo và gạch cua, hòa bột năng với nước lạnh rồi rót từ từ vào nồi đến khi đạt độ sánh sệt mượt mà.
- **Thịt cua nguyên miếng**: Xào thịt cua biển tươi với chút dầu màu điều và hành phi nhanh tay để cua thấm vị và giữ nguyên thớ thịt ngọt.
- **Sợi bánh canh**: Dùng bánh canh bột năng hoặc bột lọc trụng chín tới để sợi giữ được độ dẻo dai trong suốt."),

        R(
            "cao-lau-hoi-an",
            "Cao lầu Hội An",
            "mon-nuoc",
            "Sợi cao lầu dai, thịt xá xíu mềm, rau sống Trà Quế và tóp cao lầu giòn.",
            40,
            70,
            4,
            RecipeDifficulty.Hard,
            N(540, 28, 63, 20, 6, 960),
            [I("Sợi cao lầu", 700, "g"), I("Thịt ba chỉ", 600, "g"), I("Ngũ vị hương", 1, "thìa cà phê"), I("Nước tương", 3, "muỗng canh"), I("Đường thốt nốt", 2, "muỗng canh"), I("Tóp cao lầu", 120, "g"), I("Giá đỗ", 200, "g"), I("Rau sống", 300, "g")],
            [S("Ướp thịt", "Ướp ba chỉ với ngũ vị hương, nước tương, tỏi, đường và tiêu.", 30, "prep"), S("Rim thịt", "Áp chảo thịt rồi thêm nước ướp, rim nhỏ lửa đến mềm; để nguội và thái lát.", 50, "cook"), S("Chuẩn bị sợi", "Chần sợi cao lầu và giá đỗ, để ráo.", 5, "prep"), S("Trình bày", "Xếp rau, sợi, thịt và tóp giòn; chan một ít nước rim thịt đậm vị.", 5, "cook")],
            @"- **Hương vị xá xíu**: Thịt ba chỉ hoặc nạc vai ướp ngũ vị hương, tỏi băm và nước tương đậm đặc; rim nhỏ lửa đến khi nước cạn sánh và thịt lên màu nâu cánh gián.
- **Sợi cao lầu**: Sợi cao lầu truyền thống cần trần qua nước sôi cho vừa dẻo mềm nhưng vẫn giữ độ sần sật đặc trưng.
- **Tóp giòn**: Chiên tóp cao lầu hoặc da heo giòn rụm, rải lên mặt bát cùng giá chần và rau sống Trà Quế."),

        R(
            "com-tam-suon-bi-cha",
            "Cơm tấm sườn bì chả",
            "com-va-xoi",
            "Đĩa cơm tấm Sài Gòn đủ sườn nướng, bì, chả trứng, đồ chua và mỡ hành.",
            45,
            40,
            4,
            RecipeDifficulty.Hard,
            N(720, 38, 82, 27, 5, 1180),
            [I("Gạo tấm", 500, "g"), I("Sườn cốt lết", 4, "miếng"), I("Bì heo", 200, "g"), I("Thính gạo", 30, "g"), I("Thịt heo xay", 250, "g"), I("Trứng vịt", 4, "quả"), I("Cà rốt và củ cải", 300, "g"), I("Hành lá", 60, "g")],
            [S("Ướp sườn", "Ướp sườn với sả, tỏi, nước mắm, mật ong và dầu ăn ít nhất 30 phút.", 30, "prep"), S("Làm bì và chả", "Trộn bì luộc thái sợi với thính; hấp hỗn hợp thịt xay, miến và trứng đến chín.", 30, "prep"), S("Nướng sườn", "Nướng sườn trên than hoặc chảo đến chín, xém cạnh; quét phần nước ướp khi nướng.", 15, "cook"), S("Hoàn thiện", "Dọn cơm tấm với sườn, bì, chả, đồ chua, mỡ hành và nước mắm pha.", 10, "cook")],
            @"- **Ướp sườn nướng**: Dùng sữa đặc hoặc mật ong kết hợp nước mắm ngon, tỏi sả băm nhuyễn và chút dầu ăn để ướp sườn; nướng than hoa ở nhiệt độ vừa để thịt chín mọng mềm.
- **Chả trứng béo mềm**: Trộn thịt xay, nấm mèo băm, miến ngâm và trứng vịt; hấp cách thủy và quét lòng đỏ trứng pha dầu điều lên mặt ở 5 phút cuối để lên màu vàng đẹp.
- **Đồ chua và mỡ hành**: Cà rốt củ cải ngâm giấm đường giòn ngọt cùng chén mỡ hành xanh mướt giúp cân bằng độ béo ngậy của đĩa cơm."),

        R(
            "com-ga-hoi-an",
            "Cơm gà Hội An",
            "com-va-xoi",
            "Cơm vàng thơm nghệ nấu bằng nước luộc gà, ăn cùng gà xé và rau răm hành tây.",
            35,
            55,
            4,
            RecipeDifficulty.Medium,
            N(610, 36, 68, 21, 4, 870),
            [I("Gà ta", 1.2m, "kg"), I("Gạo", 500, "g"), I("Nghệ tươi", 20, "g"), I("Hành tây", 1, "củ"), I("Rau răm", 50, "g"), I("Đu đủ xanh", 250, "g"), I("Tỏi", 4, "tép"), I("Chanh", 2, "quả")],
            [S("Luộc gà", "Luộc gà với gừng, hành và muối ở lửa nhẹ; ngâm gà trong nước luộc 10 phút rồi để nguội.", 35, "cook"), S("Nấu cơm", "Xào gạo với nghệ và mỡ gà, nấu bằng nước luộc gà đến chín tơi.", 25, "cook"), S("Trộn gà", "Xé gà, trộn với hành tây, rau răm, chanh, muối tiêu và chút nước mắm.", 10, "prep"), S("Trình bày", "Dọn cơm với gà trộn, đu đủ chua và chén nước luộc gà.", 5, "cook")],
            @"- **Nấu cơm vàng thơm**: Vo gạo để ráo, xào săn hạt gạo với mỡ gà và nước cốt nghệ tươi rồi nấu bằng chính nước luộc gà béo ngậy.
- **Trộn gà chua ngọt**: Gà ta luộc chín tới xé phay, bóp cùng hành tây thái mỏng ngâm đá, rau răm tươi, nước cốt chanh và muối tiêu mịn.
- **Thưởng thức**: Dọn cơm cùng đĩa gà bóp, chén tương ớt Hội An rim cay ngọt và dưa leo giòn mát."),

        R(
            "com-hen-hue",
            "Cơm hến Huế",
            "com-va-xoi",
            "Cơm nguội trộn hến xào, rau thơm, hoa chuối, đậu phộng và nước hến cay nồng.",
            30,
            35,
            4,
            RecipeDifficulty.Medium,
            N(445, 23, 58, 14, 7, 990),
            [I("Hến tươi", 1.5m, "kg"), I("Cơm nguội", 600, "g"), I("Hoa chuối bào", 250, "g"), I("Khế chua", 2, "quả"), I("Đậu phộng rang", 80, "g"), I("Da heo chiên", 100, "g"), I("Mắm ruốc Huế", 1, "muỗng canh"), I("Rau thơm", 150, "g")],
            [S("Luộc hến", "Ngâm hến cho sạch cát, luộc đến mở miệng; đãi lấy thịt và giữ nước luộc trong.", 20, "cook"), S("Xào hến", "Phi hành, xào hến nhanh với mắm ruốc, ớt và nước mắm.", 8, "cook"), S("Chuẩn bị rau", "Rửa hoa chuối, khế và rau thơm; rang đậu phộng, chiên da heo giòn.", 10, "prep"), S("Trộn cơm", "Cho cơm nguội vào tô, thêm hến, rau, khế, đậu phộng và da heo; dùng kèm nước hến nóng.", 5, "prep")],
            @"- **Xử lý hến tươi**: Ngâm hến với ớt tươi cho nhả sạch cát, luộc chín đãi lấy ruột hến; giữ lại nước luộc trong đun nóng làm nước canh ăn kèm.
- **Xào hến đậm vị**: Xào hến trên lửa lớn cùng mắm ruốc, hành phi, ớt sa tế để ruột hến săn chắc và thơm lừng.
- **Đặc trưng cơm nguội**: Cơm hến chuẩn vị phải dùng cơm trắng để nguội, trộn đều cùng hoa chuối bào, khế chua, da heo chiên giòn và đậu phộng rang."),

        R(
            "xoi-gac",
            "Xôi gấc",
            "com-va-xoi",
            "Xôi nếp dẻo thơm, màu đỏ cam tự nhiên từ gấc, thích hợp cho dịp lễ và mâm cỗ.",
            20,
            45,
            6,
            RecipeDifficulty.Easy,
            N(390, 7, 70, 9, 3, 120),
            [I("Gạo nếp", 700, "g"), I("Thịt gấc", 250, "g"), I("Nước cốt dừa", 150, "ml"), I("Đường", 80, "g"), I("Muối", 1, "thìa cà phê"), I("Dầu ăn", 1, "muỗng canh")],
            [S("Ngâm nếp", "Vo nếp, ngâm 6–8 giờ rồi để thật ráo.", 480, "prep"), S("Trộn gấc", "Bóp thịt gấc với một ít rượu trắng, trộn đều cùng nếp và muối.", 10, "prep"), S("Đồ xôi", "Hấp nếp 30 phút, rưới nước cốt dừa rồi hấp thêm đến khi hạt nếp dẻo trong.", 45, "cook"), S("Hoàn thiện", "Trộn đường và dầu khi xôi còn nóng, xới nhẹ để hạt nếp bóng mà không nát.", 5, "cook")],
            @"- **Xử lý thịt gấc**: Bóp nhuyễn thịt gấc tươi với một thìa rượu trắng để kích màu đỏ tươi và dậy hương thơm nồng nàn.
- **Kỹ thuật đồ xôi**: Ngâm nếp cái hoa vàng từ 6-8 tiếng; hấp lửa vừa trong 30-40 phút, rưới nước cốt dừa và xới đều 2 lần để hạt nếp căng bóng dẻo thơm.
- **Hoàn thiện**: Trộn đường cát khi xôi còn nóng hổi để đường tan đều, giúp xôi ngọt thanh và hạt nếp mềm mướt nhiều giờ sau."),

        R(
            "xoi-man",
            "Xôi mặn",
            "com-va-xoi",
            "Xôi dẻo phủ lạp xưởng, gà xé, chà bông, trứng cút, hành phi và mỡ hành.",
            25,
            45,
            4,
            RecipeDifficulty.Medium,
            N(590, 22, 72, 24, 3, 940),
            [I("Gạo nếp", 500, "g"), I("Lạp xưởng", 200, "g"), I("Ức gà", 250, "g"), I("Trứng cút", 12, "quả"), I("Chà bông", 80, "g"), I("Hành tím", 6, "củ"), I("Hành lá", 50, "g"), I("Nước tương", 2, "muỗng canh")],
            [S("Đồ xôi", "Ngâm nếp qua đêm, để ráo rồi hấp đến dẻo mềm.", 40, "cook"), S("Chuẩn bị đồ mặn", "Luộc và xé gà; áp chảo lạp xưởng, luộc trứng cút; phi hành tím vàng.", 20, "prep"), S("Nêm xôi", "Trộn xôi nóng với mỡ hành và một ít nước tương cho vừa vị.", 5, "cook"), S("Trình bày", "Xếp lạp xưởng, gà, trứng cút, chà bông và hành phi lên xôi.", 5, "cook")],
            @"- **Đồ xôi dẻo ráo**: Hấp nếp chín tới, tránh để nước đọng dưới đáy xửng làm nát hạt xôi.
- **Topping đa dạng**: Lạp xưởng áp chảo thái lát xéo, ức gà luộc xé sợi rim mặn ngọt, chà bông tơi xốp và trứng cút luộc chiên sơ.
- **Hương vị béo ngậy**: Rưới mỡ hành lá phi thơm và chan nhẹ chút nước tương tỏi ớt khi dọn ăn nóng."),

        R(
            "com-chien-hai-san",
            "Cơm chiên hải sản",
            "com-va-xoi",
            "Cơm nguội chiên tơi cùng tôm, mực, trứng và rau củ, thơm mùi hành lá.",
            20,
            15,
            4,
            RecipeDifficulty.Easy,
            N(520, 25, 67, 17, 4, 760),
            [I("Cơm nguội", 700, "g"), I("Tôm", 250, "g"), I("Mực", 250, "g"), I("Trứng gà", 3, "quả"), I("Cà rốt", 100, "g"), I("Đậu Hà Lan", 100, "g"), I("Hành lá", 50, "g"), I("Nước mắm", 1, "muỗng canh")],
            [S("Chuẩn bị", "Làm sạch tôm mực, cắt vừa ăn; đánh trứng, cắt hạt lựu cà rốt.", 15, "prep"), S("Xào hải sản", "Làm nóng chảo, xào tôm mực vừa chín rồi trút ra.", 4, "cook"), S("Chiên cơm", "Đảo trứng, thêm cơm và rau củ, chiên lửa lớn đến hạt cơm tơi.", 8, "cook"), S("Hoàn thiện", "Cho hải sản lại chảo, nêm nước mắm và tiêu, đảo cùng hành lá.", 3, "cook")],
            @"- **Xử lý cơm**: Dùng cơm nguội để qua đêm trong ngăn mát tủ lạnh, bóp tơi hạt cơm cùng lòng đỏ trứng gà trước khi chiên để hạt cơm áo màu vàng óng.
- **Chiên lửa lớn**: Xào hải sản (tôm, mực) chín tới rồi vớt riêng; chiên cơm trên chảo gang lửa lớn đảo liên tục để hạt cơm săn giòn ngoài mặt mềm bên trong.
- **Hoàn thiện**: Đổ hải sản và rau củ hạt lựu vào đảo nhanh tay cùng hành lá cắt nhỏ và tiêu xay thơm nồng."),

        R(
            "ca-kho-to",
            "Cá kho tộ",
            "mon-kho-va-rim",
            "Cá lóc kho trong nồi đất với nước màu, nước mắm và tiêu đến khi thấm đậm.",
            20,
            45,
            4,
            RecipeDifficulty.Medium,
            N(330, 31, 9, 18, 1, 980),
            [I("Cá lóc", 900, "g"), I("Thịt ba chỉ", 150, "g"), I("Nước dừa tươi", 300, "ml"), I("Nước mắm", 4, "muỗng canh"), I("Đường", 2, "muỗng canh"), I("Hành tím", 4, "củ"), I("Tiêu đen", 1, "thìa cà phê"), I("Ớt", 2, "quả")],
            [S("Ướp cá", "Cắt cá thành khoanh, ướp nước mắm, hành tím, tiêu và một phần đường.", 20, "prep"), S("Làm nước màu", "Thắng phần đường còn lại đến màu cánh gián, thêm ba chỉ đảo săn.", 5, "cook"), S("Kho cá", "Xếp cá vào nồi đất, thêm nước dừa; kho lửa nhỏ đến khi nước sánh và cá thấm.", 40, "cook"), S("Hoàn thiện", "Rắc thêm tiêu, hành lá và ớt, dùng nóng với cơm.", 2, "cook")],
            @"- **Nước màu tự nhiên**: Thắng đường cát với chút dầu ăn đến màu cánh gián đậm hổ phách, cho hành tỏi vào phi thơm rồi mới thả cá vào đảo nhẹ.
- **Kho hai lửa**: Kho cá lóc trong tộ đất với nước dừa tươi trên lửa liu riu; kho lần một rồi tắt bếp để cá nguội ngấm gia vị, trước khi ăn kho lửa hai cho keo quánh.
- **Thịt mỡ béo ngậy**: Thêm vài miếng thịt ba chỉ thái dày kho cùng để mỡ tươm ra giúp miếng cá bóng mượt và thịt cá bùi béo."),

        R(
            "thit-kho-trung",
            "Thịt kho trứng",
            "mon-kho-va-rim",
            "Thịt ba chỉ mềm trong, trứng thấm nước dừa và nước mắm, vị mặn ngọt hài hòa.",
            25,
            90,
            6,
            RecipeDifficulty.Medium,
            N(505, 28, 14, 37, 1, 1030),
            [I("Thịt ba chỉ", 1, "kg"), I("Trứng vịt", 8, "quả"), I("Nước dừa tươi", 1, "lít"), I("Nước mắm", 5, "muỗng canh"), I("Đường", 3, "muỗng canh"), I("Hành tím", 5, "củ"), I("Tỏi", 4, "tép"), I("Ớt", 2, "quả")],
            [S("Ướp thịt", "Cắt thịt miếng lớn, ướp nước mắm, hành tỏi giã, tiêu và một ít đường.", 30, "prep"), S("Luộc trứng", "Luộc chín trứng, ngâm nước lạnh rồi bóc vỏ.", 12, "cook"), S("Kho thịt", "Thắng nước màu, đảo thịt săn; thêm nước dừa và kho lửa nhỏ, thường xuyên hớt bọt.", 60, "cook"), S("Thêm trứng", "Cho trứng vào kho thêm 25–30 phút để thấm mà lòng trắng không chai.", 30, "cook")],
            @"- **Chọn và thái thịt**: Chọn thịt ba chỉ hoặc thịt đùi có lớp mỡ và nạc liền nhau, thái khối vuông khoảng 3-4 cm.
- **Nước dừa tươi**: Dùng 100% nước dừa tươi xiêm ngọt lành để kho; hớt sạch bọt trong 30 phút đầu giúp nước thịt trong veo và màu đỏ nâu bóng bẩy.
- **Bảo quản trứng**: Cho trứng luộc vào kho sau khi thịt đã mềm được 60 phút để lòng trắng trứng không bị chai cứng."),

        R(
            "tom-rim-man-ngot",
            "Tôm rim mặn ngọt",
            "mon-kho-va-rim",
            "Tôm nguyên vỏ rim nước mắm và đường đến bóng đỏ, thịt chắc và thấm vị.",
            15,
            18,
            4,
            RecipeDifficulty.Easy,
            N(260, 27, 17, 9, 1, 1010),
            [I("Tôm sú", 700, "g"), I("Nước mắm", 3, "muỗng canh"), I("Đường", 2, "muỗng canh"), I("Tỏi", 5, "tép"), I("Hành tím", 3, "củ"), I("Dầu điều", 1, "thìa cà phê"), I("Tiêu", 0.5m, "thìa cà phê"), I("Hành lá", 30, "g")],
            [S("Sơ chế tôm", "Cắt râu, rút chỉ lưng, rửa tôm và để thật ráo.", 10, "prep"), S("Tạo màu", "Phi hành tỏi với dầu điều, cho đường vào đảo đến màu hổ phách nhạt.", 3, "cook"), S("Rim tôm", "Cho tôm và nước mắm vào, đảo lửa vừa đến khi tôm đỏ và nước rim bám quanh vỏ.", 12, "cook"), S("Hoàn thiện", "Rắc tiêu và hành lá, tắt bếp khi tôm vừa săn để không bị khô.", 2, "cook")],
            @"- **Sơ chế tôm**: Cắt bỏ râu và gai nhọn đầu tôm, rút chỉ đen lưng nhưng giữ nguyên vỏ để khi rim tôm giòn vỏ và chắc thịt.
- **Kỹ thuật rim**: Rim lửa vừa với nước mắm ngon và đường vàng đến khi nước sốt cạn dần, đảo đều tay để sốt kẹo bám đều từng con tôm đỏ au.
- **Lửa cuối**: Rưới thêm nửa muỗng dầu ăn ở bước cuối để vỏ tôm bóng bẩy, rắc hành lá và tiêu đen đập dập."),

        R(
            "ga-kho-gung",
            "Gà kho gừng",
            "mon-kho-va-rim",
            "Gà ta kho săn với nhiều gừng thái sợi, nước mắm và tiêu, ấm vị và thơm nồng.",
            20,
            35,
            4,
            RecipeDifficulty.Easy,
            N(365, 35, 8, 21, 1, 850),
            [I("Gà ta", 900, "g"), I("Gừng", 100, "g"), I("Nước mắm", 3, "muỗng canh"), I("Đường", 1.5m, "muỗng canh"), I("Hành tím", 4, "củ"), I("Tỏi", 3, "tép"), I("Ớt", 2, "quả"), I("Tiêu", 1, "thìa cà phê")],
            [S("Ướp gà", "Chặt gà miếng vừa, ướp nước mắm, hành tỏi, tiêu và một nửa lượng gừng.", 20, "prep"), S("Xào săn", "Thắng đường màu cánh gián, cho gà vào đảo lửa vừa đến săn mặt.", 8, "cook"), S("Kho gà", "Thêm ít nước nóng và phần gừng còn lại, kho lửa nhỏ đến gà mềm và nước cạn sánh.", 25, "cook"), S("Hoàn thiện", "Nếm lại cho mặn ngọt vừa, thêm ớt và tiêu trước khi dọn.", 2, "cook")],
            @"- **Chọn gà ta**: Nên dùng gà ta chắc thịt, chặt miếng vừa ăn; ướp kỹ với nước mắm, tiêu và một nửa lượng gừng thái sợi.
- **Phi gừng thơm**: Phi vàng phần gừng còn lại với chút dầu màu điều trước khi trút thịt gà vào xào săn trên lửa lớn.
- **Độ sệt**: Đun nhỏ lửa đến khi nước kho cạn sánh sệt quyện chặt quanh từng miếng gà nâu vàng ấm nóng."),

        R(
            "bo-kho",
            "Bò kho",
            "mon-ham",
            "Bò hầm mềm với sả, hồi, cà rốt và gia vị bò kho, dùng cùng bánh mì hoặc hủ tiếu.",
            35,
            120,
            6,
            RecipeDifficulty.Hard,
            N(480, 37, 28, 25, 5, 970),
            [I("Nạm bò", 1.2m, "kg"), I("Cà rốt", 500, "g"), I("Sả", 5, "cây"), I("Gừng", 40, "g"), I("Hoa hồi", 3, "cánh"), I("Bột gia vị bò kho", 2, "muỗng canh"), I("Nước dừa tươi", 700, "ml"), I("Bánh mì", 6, "ổ")],
            [S("Ướp bò", "Cắt bò khối lớn, ướp gia vị bò kho, tỏi, gừng, nước mắm và dầu điều.", 30, "prep"), S("Xào bò", "Phi sả, cho bò vào xào lửa lớn đến săn đều các mặt.", 10, "cook"), S("Hầm mềm", "Thêm nước dừa, nước nóng và hoa hồi; hầm nhỏ lửa đến khi bò gần mềm.", 90, "cook"), S("Thêm cà rốt", "Cho cà rốt vào hầm thêm 20 phút, nêm lại rồi dùng với bánh mì và rau quế.", 20, "cook")],
            @"- **Ướp nạm bò**: Chọn nạm bò có gân giòn, thái khối quân cờ lớn; ướp cùng bột gia vị bò kho, dầu màu điều, sả băm và nước mắm cốt.
- **Hầm lửa nhỏ**: Xào săn thịt bò rồi hầm cùng nước dừa tươi, hoa hồi và sả đập dập; ninh liu riu 1.5 - 2 tiếng đến khi gân bò mềm dẻo.
- **Thưởng thức**: Dùng nóng với bánh mì giòn rụm hoặc hủ tiếu trụng, kèm rau húng quế và đĩa muối tiêu chanh tươi."),

        R(
            "ca-bong-kho-tieu",
            "Cá bống kho tiêu",
            "mon-kho-va-rim",
            "Cá bống nhỏ kho keo cùng nước mắm và nhiều tiêu, thịt cá chắc, vị đậm đà.",
            20,
            35,
            4,
            RecipeDifficulty.Medium,
            N(285, 29, 10, 14, 1, 960),
            [I("Cá bống", 700, "g"), I("Nước mắm", 3, "muỗng canh"), I("Đường", 2, "muỗng canh"), I("Hành tím", 4, "củ"), I("Tiêu sọ", 2, "thìa cà phê"), I("Ớt", 3, "quả"), I("Nước dừa", 250, "ml"), I("Hành lá", 30, "g")],
            [S("Làm sạch cá", "Đánh vảy, bỏ ruột cá bống, rửa nhẹ với nước muối rồi để ráo.", 15, "prep"), S("Ướp cá", "Ướp cá với nước mắm, hành tím, đường, ớt và một nửa lượng tiêu.", 20, "prep"), S("Kho cá", "Thắng nước màu, xếp cá vào nồi, thêm nước dừa và kho lửa nhỏ không đảo mạnh.", 30, "cook"), S("Kho keo", "Mở nắp, kho đến khi nước sánh; rắc tiêu và hành lá.", 5, "cook")],
            @"- **Làm sạch cá bống**: Rửa cá bống với nước muối loãng và giấm để sạch nhớt, xốc nhẹ tránh làm nát cá rồi để thật ráo.
- **Ướp đậm đà**: Ướp cá với nước mắm ngon, đường, tiêu sọ đập dập và hành tím băm ít nhất 20 phút trước khi bắc lên bếp.
- **Kho keo trong nồi đất**: Kho cá trên lửa nhỏ liu riu, không dùng đũa đảo mạnh; mở nắp ở những phút cuối để nước kho keo sánh phủ bóng thân cá."),

        R(
            "rau-muong-xao-toi",
            "Rau muống xào tỏi",
            "mon-xao",
            "Rau muống xanh giòn xào nhanh với tỏi vàng, món rau quen thuộc của bữa cơm Việt.",
            15,
            7,
            4,
            RecipeDifficulty.Easy,
            N(145, 5, 13, 9, 5, 520),
            [I("Rau muống", 700, "g"), I("Tỏi", 8, "tép"), I("Dầu ăn", 2, "muỗng canh"), I("Nước mắm", 1, "muỗng canh"), I("Dầu hào", 1, "muỗng canh"), I("Đường", 0.5m, "thìa cà phê")],
            [S("Nhặt rau", "Bỏ phần già, ngắt rau dài khoảng 8 cm, rửa sạch và để ráo.", 12, "prep"), S("Chần rau", "Chần rau 30 giây trong nước sôi có chút muối, vớt ngay vào nước lạnh.", 2, "cook"), S("Xào nhanh", "Phi thơm tỏi trong chảo rất nóng, cho rau và gia vị vào đảo liên tục.", 3, "cook"), S("Hoàn thiện", "Tắt bếp khi rau vừa thấm và còn xanh giòn, dùng ngay.", 1, "cook")],
            @"- **Chần nhanh giữ màu xanh**: Chần rau muống trong nồi nước sôi có chút muối hạt trong 30 giây, vớt ra ngâm ngay vào âu nước đá lạnh.
- **Lửa thật lớn**: Đun chảo thật nóng với nhiều dầu ăn, phi thơm 2/3 lượng tỏi đập dập rồi trút rau ráo nước vào đảo thật nhanh tay.
- **Tỏi phi giòn**: Cho phần tỏi còn lại vào đảo đều trước khi tắt bếp để đĩa rau dậy mùi thơm nức và giòn ngọt tự nhiên."),

        R(
            "bo-luc-lac",
            "Bò lúc lắc",
            "mon-xao",
            "Thịt bò cắt khối áp chảo xém mặt, xào cùng hành và ớt chuông, dùng với muối tiêu chanh.",
            25,
            12,
            4,
            RecipeDifficulty.Medium,
            N(410, 34, 18, 24, 3, 760),
            [I("Thăn bò", 650, "g"), I("Ớt chuông", 2, "quả"), I("Hành tây", 1, "củ"), I("Tỏi", 5, "tép"), I("Dầu hào", 2, "muỗng canh"), I("Nước tương", 1, "muỗng canh"), I("Cà chua", 2, "quả"), I("Xà lách xoong", 150, "g")],
            [S("Ướp bò", "Cắt bò khối 2,5 cm, ướp tỏi, dầu hào, nước tương, đường và tiêu.", 20, "prep"), S("Áp chảo bò", "Chia bò thành hai mẻ, áp chảo rất nóng đến xém mặt nhưng bên trong còn mọng.", 5, "cook"), S("Xào rau củ", "Xào nhanh hành tây và ớt chuông, cho bò trở lại đảo đều.", 3, "cook"), S("Trình bày", "Dọn bò trên xà lách xoong và cà chua, kèm muối tiêu chanh.", 3, "cook")],
            @"- **Chọn và ướp bò**: Dùng thăn bò mềm thái khối vuông 2-2.5 cm; ướp dầu hào, tỏi băm, tiêu và một thìa dầu ăn giúp thịt mềm mọng khi áp chảo.
- **Nhiệt chảo cực cao**: Làm nóng chảo gang thật rực, cho ít bơ lạt rồi trút thịt bò vào lắc đều tay trong 2-3 phút để mặt ngoài xém cạnh mà bên trong vẫn hồng mọng.
- **Trình bày**: Xào nhanh hành tây, ớt chuông trút ra đĩa xà lách xoong và cà chua thái lát; chấm muối tiêu chanh nguyên bản."),

        R(
            "mien-xao-cua",
            "Miến xào cua",
            "mon-xao",
            "Miến dong dai mềm xào cùng thịt cua, nấm mèo và rau củ mà không bị bết.",
            25,
            15,
            4,
            RecipeDifficulty.Medium,
            N(430, 25, 58, 12, 5, 780),
            [I("Miến dong", 300, "g"), I("Thịt cua", 350, "g"), I("Nấm mèo khô", 25, "g"), I("Cà rốt", 1, "củ"), I("Hành tây", 1, "củ"), I("Hành lá", 50, "g"), I("Nước mắm", 1.5m, "muỗng canh"), I("Dầu ăn", 2, "muỗng canh")],
            [S("Ngâm miến", "Ngâm miến trong nước mát đến vừa mềm, cắt ngắn và trộn chút dầu.", 12, "prep"), S("Xào cua", "Phi hành, xào thịt cua với nước mắm và tiêu rồi trút ra.", 4, "cook"), S("Xào miến", "Xào cà rốt, nấm và hành tây; thêm miến cùng ít nước dùng, đảo đến trong mềm.", 7, "cook"), S("Hoàn thiện", "Cho cua trở lại, đảo cùng hành lá và ngò, nếm vừa vị.", 2, "cook")],
            @"- **Xử lý miến dong**: Ngâm miến trong nước mát đến khi vừa mềm tay, vớt ráo cắt khúc ngắn rồi trộn một muỗng dầu ăn để sợi không dính bết.
- **Xào thịt cua**: Xào thịt cua biển tươi với hành phi và tiêu thơm lừng rồi trút ra đĩa riêng, không xào chung ngay từ đầu tránh nát thịt cua.
- **Xào tơi sợi**: Xào rau củ rồi cho miến vào đảo liên tục trên lửa vừa, rưới từng muỗng nước dùng gà cho sợi miến nở trong veo dai mềm."),

        R(
            "muc-xao-dua",
            "Mực xào dứa",
            "mon-xao",
            "Mực giòn ngọt xào với dứa chua dịu, cà chua, cần tây và hành tây.",
            20,
            10,
            4,
            RecipeDifficulty.Easy,
            N(245, 28, 20, 7, 4, 690),
            [I("Mực ống", 600, "g"), I("Dứa", 350, "g"), I("Cà chua", 2, "quả"), I("Cần tây", 100, "g"), I("Hành tây", 1, "củ"), I("Tỏi", 4, "tép"), I("Nước mắm", 1, "muỗng canh"), I("Dầu ăn", 2, "muỗng canh")],
            [S("Sơ chế mực", "Làm sạch, khứa vảy rồng và cắt mực miếng vừa; chần nhanh rồi để ráo.", 12, "prep"), S("Xào mực", "Phi tỏi trong chảo nóng, xào mực lửa lớn khoảng 90 giây rồi trút ra.", 2, "cook"), S("Xào rau quả", "Xào dứa, cà chua và hành tây vừa chín, nêm nước mắm và chút đường.", 4, "cook"), S("Hoàn thiện", "Cho mực và cần tây vào đảo nhanh, tắt bếp khi mực vừa giòn.", 2, "cook")],
            @"- **Khử tanh và khứa vảy rồng**: Làm sạch mực tươi, dùng dao khứa vảy rồng chéo nhẹ trên thân mực; chần nhanh qua nước sôi có gừng rồi vớt ráo.
- **Xào lửa lớn**: Xào mực riêng trên lửa lớn trong 1-2 phút cho vừa chín tới rồi trút ra; xào dứa và cà chua vừa chín tới mới cho mực trở lại.
- **Giữ độ giòn**: Thêm cần tây hành tây đảo nhanh trong 30 giây rồi tắt bếp để mực giữ được độ giòn ngọt mọng nước."),

        R(
            "canh-chua-ca-loc",
            "Canh chua cá lóc",
            "canh-va-sup",
            "Canh chua Nam Bộ hài hòa me, dứa, cà chua, bạc hà và cá lóc ngọt thịt.",
            25,
            25,
            4,
            RecipeDifficulty.Medium,
            N(230, 27, 18, 6, 5, 720),
            [I("Cá lóc", 700, "g"), I("Me chín", 60, "g"), I("Dứa", 250, "g"), I("Cà chua", 3, "quả"), I("Đậu bắp", 150, "g"), I("Bạc hà", 200, "g"), I("Giá đỗ", 150, "g"), I("Ngò om và ngò gai", 50, "g")],
            [S("Sơ chế", "Làm sạch cá, cắt khoanh; cắt dứa, cà chua, đậu bắp và bạc hà vừa ăn.", 20, "prep"), S("Nấu nước chua", "Dầm me với nước nóng, lọc lấy nước; đun cùng 1,5 lít nước và dứa.", 8, "cook"), S("Nấu cá", "Cho cá và cà chua vào, nấu nhẹ đến khi cá chín; nêm nước mắm, đường và muối.", 10, "cook"), S("Hoàn thiện", "Thêm đậu bắp, bạc hà, giá; sôi lại thì tắt bếp, rắc ngò om và ngò gai.", 5, "cook")],
            @"- **Vị chua tự nhiên**: Dầm me chín bằng nước ấm rồi lọc lấy nước cốt me; xào thơm cà chua và dứa để tạo nền nước canh chua ngọt thanh thoát.
- **Nấu cá không tanh**: Thả cá lóc cắt khoanh vào khi nước canh đang sôi bùng, nấu lửa vừa đến khi cá chín tới thì hạ nhỏ lửa.
- **Rau thơm đặc trưng**: Cho đậu bắp, bạc hà và giá đỗ vào cuối cùng; khi tắt bếp rắc nhiều ngò gai và ngò om thái nhỏ để dậy mùi thơm Nam Bộ."),

        R(
            "canh-kho-qua-nhoi-thit",
            "Canh khổ qua nhồi thịt",
            "canh-va-sup",
            "Khổ qua nhồi thịt băm và nấm mèo, hầm trong nước dùng trong đến mềm vừa.",
            35,
            45,
            6,
            RecipeDifficulty.Medium,
            N(210, 17, 11, 12, 5, 650),
            [I("Khổ qua", 5, "quả"), I("Thịt heo xay", 450, "g"), I("Nấm mèo", 20, "g"), I("Miến", 30, "g"), I("Hành tím", 3, "củ"), I("Nước mắm", 2, "muỗng canh"), I("Hành lá", 50, "g"), I("Nước dùng xương", 2, "lít")],
            [S("Chuẩn bị khổ qua", "Cắt khổ qua thành khúc, lấy sạch ruột và chần nhanh với nước muối.", 15, "prep"), S("Trộn nhân", "Trộn thịt xay với nấm mèo, miến, hành tím, nước mắm và tiêu.", 10, "prep"), S("Nhồi và hầm", "Nhồi nhân vừa chặt, xếp vào nước dùng đang sôi nhẹ và hầm đến mềm.", 40, "cook"), S("Hoàn thiện", "Hớt bọt để nước trong, nêm vừa vị và thêm hành lá trước khi dùng.", 5, "cook")],
            @"- **Giảm đắng khổ qua**: Khổ qua nạo sạch ruột trắng bên trong, chần sơ qua nước sôi có chút muối rồi vớt ra ngâm nước lạnh.
- **Trộn nhân dẻo dai**: Trộn thịt nạc vai xay với nấm mèo băm, miến ngâm và hành tím; quết đều tay để nhân kết dính chặt chẽ khi hầm.
- **Nước canh trong veo**: Hầm khổ qua trong nước dùng xương trên lửa nhỏ, mở hé nắp vung và hớt bọt thường xuyên đến khi khổ qua mềm ngọt thanh."),

        R(
            "canh-cua-rau-day",
            "Canh cua rau đay",
            "canh-va-sup",
            "Canh cua đồng với rau đay, mồng tơi và mướp, vị thanh ngọt dân dã miền Bắc.",
            30,
            20,
            4,
            RecipeDifficulty.Medium,
            N(165, 14, 13, 7, 6, 510),
            [I("Cua đồng xay", 400, "g"), I("Rau đay", 250, "g"), I("Mồng tơi", 250, "g"), I("Mướp hương", 1, "quả"), I("Mắm tôm", 1, "thìa cà phê"), I("Muối", 1, "thìa cà phê")],
            [S("Lọc cua", "Hòa cua xay với 1,2 lít nước, bóp kỹ và lọc qua rây.", 12, "prep"), S("Nấu riêu cua", "Đun lửa vừa, khuấy nhẹ ban đầu; khi riêu nổi thì gạt sang mép nồi.", 10, "cook"), S("Thêm rau", "Cho mướp vào trước, sau đó thêm rau đay và mồng tơi.", 5, "cook"), S("Nêm canh", "Nêm muối và một ít mắm tôm, đun sôi lại rồi tắt bếp.", 2, "cook")],
            @"- **Lọc cua đồng**: Giã hoặc xay nhuyễn cua đồng, hòa nước bóp kỹ rồi lọc qua rây 2-3 lần để loại bỏ sạch cặn xương.
- **Đóng tảng riêu**: Đun nước cua trên lửa vừa, khuấy nhẹ ban đầu; khi riêu bắt đầu kết tảng nổi lên thì dạt sang một bên để riêu không vỡ vụn.
- **Rau đay mồng tơi**: Cho mướp hương thái vát vào trước, sau đó cho rau đay và mồng tơi vào nấu chín tới; nêm chút mắm tôm ngon chuẩn vị Bắc."),

        R(
            "canh-bi-do-nau-tom",
            "Canh bí đỏ nấu tôm",
            "canh-va-sup",
            "Bí đỏ bùi ngọt nấu cùng tôm tươi, món canh dễ làm và hợp bữa cơm hằng ngày.",
            15,
            20,
            4,
            RecipeDifficulty.Easy,
            N(175, 13, 22, 5, 4, 460),
            [I("Bí đỏ", 700, "g"), I("Tôm tươi", 300, "g"), I("Hành tím", 2, "củ"), I("Nước mắm", 1, "muỗng canh"), I("Hành lá", 30, "g"), I("Ngò rí", 20, "g")],
            [S("Sơ chế", "Gọt bí, bỏ hạt và cắt khối; bóc tôm, băm thô một nửa.", 12, "prep"), S("Xào tôm", "Phi hành tím, xào tôm với chút nước mắm đến săn.", 3, "cook"), S("Nấu canh", "Thêm 1,2 lít nước, đun sôi rồi cho bí vào nấu đến mềm nhưng không nát.", 15, "cook"), S("Hoàn thiện", "Nêm vừa vị, thêm hành lá và ngò rí rồi tắt bếp.", 2, "cook")],
            @"- **Chọn bí đỏ**: Chọn quả bí đỏ hồ lô hoặc bí nếp già vỏ cứng, ruột vàng sẫm để khi nấu canh có độ dẻo bùi tự nhiên.
- **Giã tôm tươi**: Tôm bóc vỏ rút chỉ đen, giã thô cùng chút đầu hành trắng và tiêu xay giúp nước canh ngọt đậm đà.
- **Nấu vừa độ chín**: Nấu bí đỏ vừa mềm tới, không nấu quá nát; rắc ngò rí và hành hoa thái nhỏ trước khi múc ra tô."),

        R(
            "canh-rau-ngot-thit-bam",
            "Canh rau ngót thịt băm",
            "canh-va-sup",
            "Rau ngót vò nhẹ nấu với thịt băm, nước canh trong và có vị ngọt tự nhiên.",
            15,
            12,
            4,
            RecipeDifficulty.Easy,
            N(155, 14, 9, 7, 5, 480),
            [I("Rau ngót", 500, "g"), I("Thịt heo xay", 250, "g"), I("Hành tím", 2, "củ"), I("Nước mắm", 1, "muỗng canh"), I("Dầu ăn", 1, "thìa cà phê"), I("Tiêu", 0.5m, "thìa cà phê")],
            [S("Sơ chế rau", "Tuốt lá rau ngót, rửa sạch rồi vò nhẹ để rau mềm và thơm.", 10, "prep"), S("Xào thịt", "Phi hành tím, xào thịt băm tơi với chút nước mắm.", 3, "cook"), S("Nấu canh", "Thêm 1,2 lít nước, đun sôi; cho rau ngót vào nấu đến vừa mềm.", 6, "cook"), S("Hoàn thiện", "Nêm lại, rắc tiêu và tắt bếp để rau giữ màu xanh.", 1, "cook")],
            @"- **Vò rau ngót**: Tuốt lấy lá non, rửa sạch và vò hơi dập nhẹ bằng tay để lá rau mềm và tiết vị ngọt mát khi nấu.
- **Xào thịt săn thơm**: Xào thịt nạc băm với hành tím phi thơm trên lửa vừa cho tơi ráo trước khi châm nước sôi vào.
- **Nấu nhanh**: Thả rau ngót vào nước canh sôi, nấu bùng trong 2-3 phút rồi nêm nếm tắt bếp ngay để rau giữ màu xanh mướt."),

        R(
            "goi-cuon-tom-thit",
            "Gỏi cuốn tôm thịt",
            "mon-cuon-va-goi",
            "Cuốn bánh tráng tươi với tôm, thịt ba chỉ, bún và rau, dùng cùng tương đậu phộng.",
            35,
            20,
            4,
            RecipeDifficulty.Medium,
            N(310, 21, 39, 9, 5, 590),
            [I("Bánh tráng", 16, "miếng"), I("Tôm", 400, "g"), I("Thịt ba chỉ", 350, "g"), I("Bún tươi", 400, "g"), I("Xà lách", 150, "g"), I("Hẹ", 80, "g"), I("Rau thơm", 120, "g"), I("Tương đen", 120, "ml")],
            [S("Luộc nhân", "Luộc thịt chín, để nguội thái lát; luộc tôm vừa chín, bóc vỏ và chẻ đôi.", 20, "cook"), S("Chuẩn bị rau bún", "Rửa và để rau thật ráo; cắt bún thành đoạn ngắn.", 10, "prep"), S("Cuốn", "Làm ẩm bánh tráng, xếp rau, bún, thịt, tôm và hẹ rồi cuốn chặt tay.", 15, "cook"), S("Pha tương", "Đun tương đen với chút nước, thêm bơ đậu phộng, tỏi và ớt; rắc đậu phộng rang.", 5, "prep")],
            @"- **Luộc thịt và tôm**: Luộc thịt ba chỉ với củ hành tím đập dập, vớt ra ngâm nước đá rồi thái lát mỏng; luộc tôm vừa đỏ tới, bóc vỏ chẻ đôi dọc sống lưng.
- **Thao tác cuốn chặt tay**: Làm ẩm nhẹ bánh tráng dẻo, xếp xà lách, rau thơm, bún tươi, thịt luộc và tôm úp mặt đỏ ra ngoài; đặt nhánh hẹ ló ra đầu cuốn.
- **Nước chấm tương đậu**: Nấu tương đen cùng bơ đậu phộng, chút nước cốt dừa và tỏi ớt băm nhuyễn; rắc đậu phộng rang giã dập lên mặt."),

        R(
            "bo-bia-man",
            "Bò bía mặn",
            "mon-cuon-va-goi",
            "Bò bía cuốn lạp xưởng, trứng, củ sắn xào, tôm khô và xà lách, chấm tương đậu.",
            35,
            25,
            4,
            RecipeDifficulty.Medium,
            N(345, 15, 42, 14, 5, 720),
            [I("Bánh tráng", 16, "miếng"), I("Lạp xưởng", 200, "g"), I("Củ sắn", 500, "g"), I("Cà rốt", 150, "g"), I("Trứng gà", 3, "quả"), I("Tôm khô", 60, "g"), I("Xà lách", 150, "g"), I("Tương đen", 120, "ml")],
            [S("Chuẩn bị nhân", "Thái sợi củ sắn, cà rốt; chiên trứng mỏng rồi thái sợi, áp chảo lạp xưởng.", 15, "prep"), S("Xào củ sắn", "Xào củ sắn, cà rốt và tôm khô đến chín nhưng còn giòn.", 10, "cook"), S("Cuốn bò bía", "Làm ẩm bánh tráng, xếp xà lách, nhân xào, trứng và lạp xưởng rồi cuốn gọn.", 15, "cook"), S("Pha tương", "Nấu tương đen với bơ đậu phộng và chút nước; thêm đồ chua, ớt nếu thích.", 5, "prep")],
            @"- **Xào củ sắn giòn ngọt**: Củ sắn và cà rốt thái sợi vắt nhẹ bớt nước, xào chín tới cùng tôm khô đã ngâm mềm để giữ độ giòn sần sật.
- **Trứng tráng mỏng**: Đánh tan trứng gà tráng thật mỏng trên chảo chống dính rồi thái sợi dài đều đặn.
- **Cuốn và thưởng thức**: Xếp xà lách, rau thơm, củ sắn xào, trứng sợi và lạp xưởng thái lát lên bánh tráng cuốn chặt; chấm sốt tương đen béo ngậy."),

        R(
            "goi-ga-bap-cai",
            "Gỏi gà bắp cải",
            "mon-cuon-va-goi",
            "Gà xé trộn bắp cải giòn, cà rốt, hành tây, rau răm và nước mắm chua ngọt.",
            30,
            25,
            4,
            RecipeDifficulty.Easy,
            N(295, 29, 19, 12, 6, 720),
            [I("Ức gà", 600, "g"), I("Bắp cải", 500, "g"), I("Cà rốt", 150, "g"), I("Hành tây", 1, "củ"), I("Rau răm", 50, "g"), I("Đậu phộng rang", 70, "g"), I("Chanh", 3, "quả"), I("Nước mắm", 3, "muỗng canh")],
            [S("Luộc gà", "Luộc gà với gừng và hành đến vừa chín, để nguội rồi xé sợi.", 20, "cook"), S("Sơ chế rau", "Thái mỏng bắp cải, cà rốt và hành tây; ngâm hành trong nước đá cho bớt hăng.", 10, "prep"), S("Pha nước trộn", "Khuấy nước mắm, nước cốt chanh, đường, tỏi và ớt cho tan.", 5, "prep"), S("Trộn gỏi", "Trộn gà và rau với nước mắm, thêm rau răm; rắc đậu phộng và hành phi trước khi dùng.", 5, "prep")],
            @"- **Sơ chế bắp cải giòn**: Bắp cải bào sợi mỏng, ngâm ngay vào thau nước đá lạnh pha chút nước cốt chanh trong 10 phút rồi vớt ra vắt thật ráo.
- **Gà xé phay**: Dùng gà ta luộc chín tới, xé miếng vừa ăn có cả da giòn và thịt nạc ngọt tự nhiên.
- **Nước mắm trộn gỏi**: Pha nước mắm chua ngọt theo tỉ lệ vàng (mắm, đường, chanh, tỏi ớt băm); rưới đều bóp nhẹ tay và trộn cùng rau răm thái nhỏ."),

        R(
            "nom-hoa-chuoi",
            "Nộm hoa chuối",
            "mon-cuon-va-goi",
            "Hoa chuối giòn trộn thịt, tôm, rau thơm, đậu phộng và nước mắm chua ngọt.",
            35,
            15,
            4,
            RecipeDifficulty.Medium,
            N(285, 20, 22, 14, 8, 690),
            [I("Hoa chuối", 500, "g"), I("Thịt nạc vai", 250, "g"), I("Tôm", 250, "g"), I("Cà rốt", 120, "g"), I("Rau thơm", 100, "g"), I("Đậu phộng rang", 70, "g"), I("Chanh", 3, "quả"), I("Nước mắm", 3, "muỗng canh")],
            [S("Ngâm hoa chuối", "Bào mỏng hoa chuối, ngâm ngay trong nước chanh loãng để không thâm rồi vắt ráo.", 20, "prep"), S("Luộc thịt tôm", "Luộc thịt và tôm vừa chín; thái thịt mỏng, bóc tôm.", 15, "cook"), S("Pha nước trộn", "Khuấy nước mắm, chanh, đường, tỏi và ớt cho vị chua ngọt cân bằng.", 5, "prep"), S("Trộn nộm", "Trộn hoa chuối, cà rốt, thịt, tôm và rau thơm; rắc đậu phộng, hành phi.", 5, "prep")],
            @"- **Hoa chuối không thâm**: Bào mỏng hoa chuối ngâm ngay vào âu nước lạnh pha nước cốt chanh hoặc giấm gạo; vớt ráo nước trước khi trộn.
- **Kết hợp tôm thịt**: Luộc chín tôm sú bóc vỏ và thịt ba chỉ thái mỏng tạo vị béo bùi cân bằng độ chát nhẹ của hoa chuối.
- **Trộn nhanh tay**: Trộn đều hoa chuối, cà rốt sợi, rau thơm và nước sốt chua ngọt; rắc nhiều đậu phộng rang và hành phi giòn trước khi dùng."),

        R(
            "goi-ngo-sen-tom-thit",
            "Gỏi ngó sen tôm thịt",
            "mon-cuon-va-goi",
            "Ngó sen giòn mát trộn tôm, thịt, rau răm và nước mắm chua ngọt.",
            35,
            20,
            4,
            RecipeDifficulty.Medium,
            N(300, 24, 24, 12, 6, 710),
            [I("Ngó sen", 500, "g"), I("Tôm", 300, "g"), I("Thịt ba chỉ", 250, "g"), I("Cà rốt", 120, "g"), I("Dưa leo", 1, "quả"), I("Rau răm", 50, "g"), I("Đậu phộng rang", 70, "g"), I("Nước mắm", 3, "muỗng canh")],
            [S("Sơ chế ngó sen", "Rửa ngó sen, chẻ vừa ăn và ngâm nước giấm đường lạnh cho trắng giòn.", 20, "prep"), S("Luộc tôm thịt", "Luộc tôm và thịt riêng đến vừa chín; bóc tôm, thái thịt mỏng.", 15, "cook"), S("Pha nước trộn", "Hòa nước mắm, chanh, đường, tỏi và ớt đến khi tan.", 5, "prep"), S("Trộn gỏi", "Vắt ráo ngó sen, trộn với tôm thịt, cà rốt, dưa leo và rau răm; rắc đậu phộng.", 5, "prep")],
            @"- **Ngó sen trắng giòn**: Ngó sen cắt khúc chẻ đôi ngâm nước giấm đường lạnh để giữ độ trắng phau và giòn rụm.
- **Vắt ráo nước**: Trước khi trộn cần vắt nhẹ ngó sen để món gỏi không bị ra nước làm loãng nước xốt.
- **Thưởng thức**: Trộn đều tôm luộc, thịt ba chỉ, cà rốt bào sợi và rau răm; dùng kèm bánh phồng tôm chiên giòn rụm."),

        R(
            "banh-xeo-mien-tay",
            "Bánh xèo miền Tây",
            "banh-viet",
            "Bánh xèo lớn, vỏ mỏng giòn từ bột gạo và nghệ, nhân tôm thịt, giá và đậu xanh.",
            40,
            35,
            4,
            RecipeDifficulty.Hard,
            N(520, 22, 56, 24, 6, 760),
            [I("Bột gạo", 400, "g"), I("Nước cốt dừa", 300, "ml"), I("Bột nghệ", 1, "thìa cà phê"), I("Tôm", 350, "g"), I("Thịt ba chỉ", 300, "g"), I("Giá đỗ", 500, "g"), I("Đậu xanh hấp", 150, "g"), I("Rau sống", 500, "g")],
            [S("Pha bột", "Khuấy bột gạo, nước cốt dừa, nước, nghệ, muối và hành lá; để bột nghỉ 30 phút.", 30, "prep"), S("Chuẩn bị nhân", "Xào tôm và thịt ba chỉ vừa chín, nêm nhẹ.", 8, "prep"), S("Đổ bánh", "Làm nóng chảo, cho tôm thịt rồi tráng lớp bột thật mỏng; thêm giá và đậu xanh, đậy nắp.", 6, "cook"), S("Làm giòn", "Mở nắp, thêm ít dầu quanh viền, chiên đến giòn rồi gập đôi; dùng với rau và nước mắm.", 4, "cook")],
            @"- **Pha bột bánh giòn lâu**: Trộn bột gạo với chút bột nghệ, hành lá cắt nhỏ và nước cốt dừa béo ngậy; để bột nghỉ ít nhất 30 phút trước khi đổ.
- **Tráng bánh thật mỏng**: Chảo gang thật nóng láng mỡ heo, múc vá bột tráng xoay tròn một lớp thật mỏng quanh thành chảo; đậy vung 2 phút cho chín nhân.
- **Làm giòn viền**: Mở nắp vung rưới thêm chút dầu ăn quanh viền chảo, chiên lửa vừa đến khi vành bánh cong giòn vàng ruộm; cuốn cùng rổ rau rừng phong phú."),

        R(
            "banh-khot-vung-tau",
            "Bánh khọt Vũng Tàu",
            "banh-viet",
            "Bánh khọt nhỏ giòn đáy, mềm trong, mỗi chiếc có tôm, mỡ hành và tôm chấy.",
            35,
            30,
            4,
            RecipeDifficulty.Hard,
            N(470, 21, 51, 21, 4, 740),
            [I("Bột gạo", 350, "g"), I("Nước cốt dừa", 200, "ml"), I("Bột nghệ", 0.5m, "thìa cà phê"), I("Tôm nhỏ", 400, "g"), I("Tôm khô", 70, "g"), I("Hành lá", 60, "g"), I("Rau sống", 400, "g"), I("Dầu ăn", 100, "ml")],
            [S("Pha bột", "Trộn bột gạo, nước cốt dừa, nước, nghệ và muối; để nghỉ 30 phút.", 30, "prep"), S("Làm tôm chấy", "Ngâm tôm khô, giã bông rồi rang tơi với chút dầu.", 10, "prep"), S("Đổ bánh", "Làm nóng khuôn, cho dầu và bột vào từng ô, đặt một con tôm lên mặt rồi đậy nắp.", 6, "cook"), S("Hoàn thiện", "Chiên đến viền giòn, lấy bánh ra, thêm mỡ hành và tôm chấy; dùng với rau và nước mắm.", 4, "cook")],
            @"- **Làm nóng khuôn**: Thoa dầu đều khắp các ô khuôn bánh khọt trên bếp than hoặc gas lửa vừa cho khuôn thật nóng trước khi rót bột.
- **Đổ bột hai lần**: Rót bột gạo pha nước cốt dừa ngập 2/3 khuôn, đậy nắp cho bột se lại rồi đặt con tôm tươi lên trên, đậy nắp thêm 3-4 phút.
- **Topping tôm chấy**: Rắc tôm khô giã nhuyễn rang vàng cùng mỡ hành béo ngậy lên mặt bánh; ăn kèm rau cải xanh, đồ chua và nước mắm ớt."),

        R(
            "banh-cuon-ha-noi",
            "Bánh cuốn Hà Nội",
            "banh-viet",
            "Lớp bánh gạo hấp mỏng cuốn nhân thịt nấm, phủ hành phi và ăn cùng chả quế.",
            45,
            35,
            4,
            RecipeDifficulty.Expert,
            N(455, 22, 61, 14, 4, 880),
            [I("Bột gạo", 350, "g"), I("Bột năng", 80, "g"), I("Thịt heo xay", 350, "g"), I("Nấm mèo", 25, "g"), I("Hành tím", 8, "củ"), I("Chả quế", 300, "g"), I("Nước mắm", 4, "muỗng canh"), I("Dầu ăn", 60, "ml")],
            [S("Pha bột", "Khuấy bột gạo, bột năng, nước, muối và dầu; để bột nghỉ ít nhất 30 phút.", 30, "prep"), S("Xào nhân", "Xào thịt với nấm mèo và hành tím đến tơi, nêm nước mắm và tiêu.", 10, "cook"), S("Tráng bánh", "Tráng lớp bột rất mỏng trên vải hấp căng, đậy 40–50 giây rồi lấy bánh ra.", 15, "cook"), S("Cuốn và dọn", "Cho nhân vào, cuốn nhẹ; phủ hành phi, dùng với chả quế và nước mắm pha ấm.", 10, "cook")],
            @"- **Pha bột mỏng dẻo**: Hòa bột gạo với chút bột năng và nước lọc, để lắng gạn nước trong nhiều lần giúp bánh trong và không bị nồng mùi bột.
- **Kỹ thuật tráng hơi**: Tráng lớp bột thật mỏng trên mặt vải nồi hơi căng, đậy vung 30-40 giây; dùng thanh tre mỏng khéo léo lấy bánh ra mâm thoa dầu.
- **Dọn ăn nóng**: Cuộn nhân thịt nạc băm nấm mèo xào thơm, rắc hành phi tự làm giòn tan; ăn kèm chả quế và chén nước mắm ấm chua ngọt nhẹ nhàng."),

        R(
            "banh-beo-chen",
            "Bánh bèo chén",
            "banh-viet",
            "Bánh bèo Huế mềm mượt trong chén nhỏ, phủ tôm chấy, mỡ hành và tóp giòn.",
            35,
            30,
            6,
            RecipeDifficulty.Hard,
            N(360, 13, 52, 11, 3, 760),
            [I("Bột gạo", 350, "g"), I("Bột năng", 50, "g"), I("Tôm", 300, "g"), I("Da heo chiên", 100, "g"), I("Hành lá", 60, "g"), I("Nước mắm", 4, "muỗng canh"), I("Đường", 3, "muỗng canh"), I("Chanh", 1, "quả")],
            [S("Pha bột", "Khuấy bột gạo, bột năng, nước và muối; để nghỉ 30 phút rồi khuấy lại.", 30, "prep"), S("Làm tôm chấy", "Hấp tôm, bóc vỏ, giã bông rồi rang khô tơi với chút dầu điều.", 12, "cook"), S("Hấp bánh", "Làm nóng chén, rót lớp bột mỏng và hấp 5–7 phút đến trong.", 15, "cook"), S("Hoàn thiện", "Phủ tôm chấy, mỡ hành, da heo giòn; dùng với nước mắm chua ngọt.", 5, "cook")],
            @"- **Tạo xoáy bánh bèo**: Pha bột gạo với nước ấm và chút bột năng; xếp chén vào xửng hấp nóng trước khi khuấy đều bột rót vào từng chén.
- **Hấp lửa lớn**: Hấp bánh trên lửa lớn sôi bùng trong 7-8 phút để mặt bánh tạo vết lõm xoáy sâu đẹp mắt.
- **Rắc topping**: Thoa mỡ hành, rắc tôm chấy đỏ au và bánh mì chiên giòn hoặc da heo giòn; chan nước mắm ớt pha loãng ngọt thanh."),

        R(
            "banh-bot-loc-hue",
            "Bánh bột lọc Huế",
            "banh-viet",
            "Bánh bột năng trong dai bọc tôm thịt rim đậm vị, ăn với nước mắm ớt.",
            45,
            35,
            4,
            RecipeDifficulty.Hard,
            N(405, 18, 58, 11, 2, 820),
            [I("Bột năng", 500, "g"), I("Tôm nhỏ", 350, "g"), I("Thịt ba chỉ", 250, "g"), I("Hành tím", 4, "củ"), I("Dầu điều", 1, "muỗng canh"), I("Nước mắm", 4, "muỗng canh"), I("Đường", 2, "muỗng canh"), I("Hành lá", 50, "g")],
            [S("Rim nhân", "Cắt nhỏ thịt, rim cùng tôm, hành tím, nước mắm, đường và dầu điều đến cạn.", 20, "cook"), S("Nhồi bột", "Rót nước sôi từ từ vào bột năng, trộn và nhồi đến khối bột dẻo mịn.", 12, "prep"), S("Tạo bánh", "Chia bột, cán mỏng, đặt tôm thịt vào giữa rồi gấp và miết kín mép.", 15, "prep"), S("Luộc bánh", "Luộc đến khi bánh nổi và trong, vớt vào nước mát rồi trộn mỡ hành.", 10, "cook")],
            @"- **Nhồi bột năng nước sôi**: Dùng nước thật sôi đổ từ từ vào bột năng, nhồi nhanh tay khi bột còn nóng đến khi thành khối dẻo mịn không dính tay.
- **Rim nhân tôm thịt đậm đà**: Tôm nhỏ và thịt ba chỉ thái hạt lựu rim mặn ngọt với nước mắm ngon và tiêu đến khi cạn khô thơm nức.
- **Luộc bánh trong veo**: Luộc bánh trong nước sôi đến khi bánh nổi lên và vỏ bột trong suốt; vớt ra trộn ngay vào tô mỡ hành để bánh bóng mượt."),

        R(
            "banh-it-tran",
            "Bánh ít trần",
            "banh-viet",
            "Bánh nếp dẻo nhân đậu xanh thịt mặn, phủ mỡ hành, tôm chấy và hành phi.",
            45,
            40,
            6,
            RecipeDifficulty.Hard,
            N(420, 15, 63, 12, 5, 690),
            [I("Bột nếp", 500, "g"), I("Đậu xanh cà", 250, "g"), I("Thịt heo xay", 200, "g"), I("Tôm", 200, "g"), I("Hành tím", 6, "củ"), I("Hành lá", 60, "g"), I("Nước mắm", 3, "muỗng canh"), I("Dầu ăn", 3, "muỗng canh")],
            [S("Làm nhân", "Hấp chín đậu xanh, tán nhuyễn; xào với thịt, tôm băm và hành tím rồi vo viên.", 25, "prep"), S("Nhồi bột", "Nhồi bột nếp với nước ấm và chút muối đến dẻo mịn, để nghỉ 15 phút.", 20, "prep"), S("Tạo bánh", "Bọc bột quanh viên nhân, vo tròn và đặt lên lá chuối đã quét dầu.", 15, "prep"), S("Hấp bánh", "Hấp 15–18 phút đến khi vỏ trong dẻo, phủ mỡ hành, tôm chấy và hành phi.", 18, "cook")],
            @"- **Nhồi bột nếp dẻo mềm**: Nhồi bột nếp với nước ấm và chút muối hạt cho khối bột dẻo mịn, để bột nghỉ 15 phút trước khi nặn bánh.
- **Nhân đậu xanh thịt tôm**: Đậu xanh hấp chín tán nhuyễn xào cùng thịt băm, tôm khô và hành tím; vo tròn thành từng viên nhân đều đặn.
- **Hấp chín**: Đặt bánh lên từng miếng lá chuối thoa dầu, hấp cách thủy 15-18 phút; dọn kèm mỡ hành, tôm chấy và nước mắm chua ngọt."),

        R(
            "bun-cha-ha-noi",
            "Bún chả Hà Nội",
            "mon-nuong",
            "Chả viên và ba chỉ nướng than thơm, ngâm trong nước mắm ấm cùng đu đủ xanh.",
            40,
            30,
            4,
            RecipeDifficulty.Medium,
            N(590, 34, 63, 23, 5, 1120),
            [I("Thịt ba chỉ", 500, "g"), I("Thịt vai xay", 400, "g"), I("Bún tươi", 800, "g"), I("Đu đủ xanh", 200, "g"), I("Cà rốt", 100, "g"), I("Nước mắm", 6, "muỗng canh"), I("Đường", 5, "muỗng canh"), I("Rau sống", 400, "g")],
            [S("Ướp thịt", "Ướp ba chỉ thái mỏng và thịt xay riêng với nước mắm, đường, hành tím, tỏi và tiêu.", 30, "prep"), S("Làm đồ chua", "Bóp đu đủ và cà rốt với muối, rửa rồi ngâm giấm đường.", 20, "prep"), S("Nướng chả", "Vo thịt xay thành viên dẹt, nướng cùng ba chỉ trên than đến xém thơm và chín.", 15, "cook"), S("Pha nước chấm", "Pha nước mắm ấm chua ngọt, thêm đồ chua và chả nướng; dùng với bún và rau.", 10, "prep")],
            @"- **Ướp chả hai loại**: Chả miếng dùng thịt ba chỉ thái mỏng; chả viên dùng thịt nạc vai băm nhuyễn; ướp cùng nước màu thắng, hành khô, nước mắm ngon và tiêu xay.
- **Nướng than hoa**: Kẹp chả vào vỉ tre hoặc vỉ sắt nướng trên than hoa rực hồng, quạt đều tay và trở mặt liên tục để chả xém vàng không bị khô cháy.
- **Nước chấm ấm nóng**: Pha nước mắm, giấm, đường, nước lọc đun ấm; thả đu đủ cà rốt giòn vào bát nước chấm cùng chả nóng thơm lừng."),

        R(
            "thit-nuong-sa",
            "Thịt nướng sả",
            "mon-nuong",
            "Thịt vai ướp sả, tỏi và nước mắm, nướng xém cạnh nhưng vẫn mềm mọng.",
            30,
            18,
            4,
            RecipeDifficulty.Easy,
            N(430, 31, 20, 25, 2, 830),
            [I("Thịt vai heo", 800, "g"), I("Sả", 5, "cây"), I("Tỏi", 5, "tép"), I("Hành tím", 4, "củ"), I("Nước mắm", 3, "muỗng canh"), I("Mật ong", 2, "muỗng canh"), I("Dầu ăn", 2, "muỗng canh"), I("Bún và rau", 800, "g")],
            [S("Thái và ướp", "Thái thịt mỏng, ướp sả băm, tỏi, hành, nước mắm, mật ong và tiêu.", 30, "prep"), S("Xiên thịt", "Xiên thịt lỏng tay hoặc trải thành một lớp trên vỉ để chín đều.", 8, "prep"), S("Nướng", "Nướng than lửa vừa, trở mặt và quét dầu đến khi thịt xém cạnh, chín mọng.", 12, "cook"), S("Trình bày", "Dùng với bún, đồ chua, rau thơm, mỡ hành và nước mắm chua ngọt.", 5, "cook")],
            @"- **Thịt nạc vai mọng mềm**: Chọn thịt nạc vai có xen kẽ vân mỡ thái lát vừa ăn, ướp cùng sả băm nhuyễn, tỏi, nước mắm, mật ong và chút dầu hào.
- **Thời gian ngấm vị**: Ướp thịt ít nhất 1-2 tiếng trong ngăn mát tủ lạnh để gia vị ngấm sâu vào từng thớ thịt.
- **Nướng vàng óng**: Nướng trên than hoa hoặc nồi chiên không dầu ở 180°C, quét nước ướp pha mật ong ở 5 phút cuối để thịt lên màu cánh gián bắt mắt."),

        R(
            "ga-nuong-muoi-ot",
            "Gà nướng muối ớt",
            "mon-nuong",
            "Gà nguyên con ướp muối ớt, sả và mật ong, nướng da giòn, thịt mọng.",
            35,
            55,
            6,
            RecipeDifficulty.Medium,
            N(420, 42, 10, 24, 1, 860),
            [I("Gà ta", 1.6m, "kg"), I("Ớt sừng", 5, "quả"), I("Sả", 4, "cây"), I("Tỏi", 6, "tép"), I("Muối hột", 1, "muỗng canh"), I("Mật ong", 2, "muỗng canh"), I("Dầu điều", 1, "muỗng canh"), I("Chanh", 2, "quả")],
            [S("Pha sốt ướp", "Giã ớt, sả, tỏi và muối; trộn với mật ong, dầu điều và chút nước mắm.", 10, "prep"), S("Ướp gà", "Mổ phanh gà, thấm khô rồi xoa sốt cả ngoài da lẫn mặt trong; ướp ít nhất 2 giờ.", 120, "prep"), S("Nướng gà", "Nướng 180°C hoặc trên than lửa vừa, trở đều và quét sốt đến khi da vàng giòn.", 50, "cook"), S("Nghỉ thịt", "Để gà nghỉ 8 phút trước khi chặt, dùng với dưa leo, rau thơm và muối tiêu chanh.", 8, "cook")],
            @"- **Sơ chế và khử mùi**: Chà xát gà với muối hạt và chanh tươi, mổ phanh ức gà ép dẹp để gà chín đều khi nướng.
- **Xốt muối ớt cay nồng**: Giã nhuyễn ớt sừng, ớt hiểm cùng muối hạt, tỏi băm, dầu màu điều và mật ong; xoa đều khắp mình gà cả trong lẫn ngoài.
- **Nướng da giòn**: Nướng gà ở nhiệt độ 180-200°C, trở đều hai mặt đến khi da gà vàng ruộm, thịt chín mọng; chấm muối tiêu chanh hoặc muối ớt xanh."),

        R(
            "ca-loc-nuong-trui",
            "Cá lóc nướng trui",
            "mon-nuong",
            "Cá lóc nướng nguyên con bằng rơm hoặc than, thịt ngọt cuốn bánh tráng và rau đồng.",
            25,
            35,
            4,
            RecipeDifficulty.Hard,
            N(350, 41, 16, 13, 4, 620),
            [I("Cá lóc", 1.2m, "kg"), I("Bánh tráng", 20, "miếng"), I("Bún tươi", 500, "g"), I("Dứa", 250, "g"), I("Dưa leo", 2, "quả"), I("Rau sống", 500, "g"), I("Me chín", 80, "g"), I("Nước mắm", 4, "muỗng canh")],
            [S("Chuẩn bị cá", "Giữ nguyên vảy, rửa sạch nhớt và xiên cá từ miệng đến đuôi để giữ thẳng.", 15, "prep"), S("Nướng cá", "Phủ rơm hoặc nướng than đến khi lớp vảy cháy đen, thịt bên trong chín thơm.", 30, "cook"), S("Làm nước chấm", "Dầm me với nước nóng, lọc rồi pha nước mắm, đường, tỏi và ớt.", 8, "prep"), S("Trình bày", "Gỡ lớp da cháy, lấy thịt cá cuốn bánh tráng với bún, dứa, dưa leo và rau.", 8, "cook")],
            @"- **Nướng nguyên con dân dã**: Cá lóc đồng để nguyên vảy và ruột, dùng que tre xiên từ miệng đến đuôi cắm xuống đất rồi phủ rơm khô đốt cháy rực.
- **Cạo sạch lớp vảy cháy**: Khi rơm tàn, rút cá ra dùng dao cạo sạch lớp vảy ngoài bị cháy đen để lộ phần thịt cá trắng ngần thơm lừng mùi khói rơm.
- **Thưởng thức**: Cuốn thịt cá cùng bánh tráng, bún tươi, khế chua, chuối chát và các loại rau đồng; chấm mắm nêm đậm đà."),

        R(
            "dau-hu-kho-nam",
            "Đậu hũ kho nấm",
            "mon-chay",
            "Đậu hũ vàng kho cùng nấm hương và nấm rơm trong nước tương mặn ngọt vừa vị.",
            20,
            25,
            4,
            RecipeDifficulty.Easy,
            N(280, 17, 20, 17, 6, 680),
            [I("Đậu hũ", 600, "g"), I("Nấm hương tươi", 200, "g"), I("Nấm rơm", 250, "g"), I("Nước dừa", 300, "ml"), I("Nước tương", 3, "muỗng canh"), I("Đường", 1, "muỗng canh"), I("Gừng", 20, "g"), I("Hành lá", 40, "g")],
            [S("Sơ chế", "Cắt đậu hũ thành khối, áp chảo vàng; làm sạch và cắt nấm vừa ăn.", 15, "prep"), S("Xào nấm", "Phi gừng, xào nấm lửa lớn đến thơm và vừa ra nước.", 5, "cook"), S("Kho", "Thêm đậu hũ, nước dừa, nước tương và đường; kho nhỏ lửa đến khi nước sánh.", 18, "cook"), S("Hoàn thiện", "Nêm lại, thêm tiêu và hành lá hoặc hành boa-rô.", 2, "cook")],
            @"- **Chiên đậu hũ vàng đều**: Cắt đậu hũ thành khối vuông vừa ăn, chiên vàng giòn các mặt trên lửa vừa để đậu không bị vỡ khi kho.
- **Sơ chế nấm**: Dùng nấm đùi gà, nấm rơm hoặc nấm đông cô tươi ngâm rửa nước muối sạch rồi khía nhẹ trên mũ nấm để ngấm sốt kho.
- **Kho lửa liu riu**: Kho đậu và nấm cùng nước tương ngon, dầu hào chay và chút nước dừa tươi đến khi nước kho sánh sệt óng ả."),

        R(
            "bun-hue-chay",
            "Bún Huế chay",
            "mon-chay",
            "Nước dùng rau củ thơm sả và sa tế, ăn cùng đậu hũ, nấm, tàu hũ ky và bún sợi lớn.",
            35,
            65,
            6,
            RecipeDifficulty.Medium,
            N(380, 16, 56, 12, 8, 890),
            [I("Bún sợi lớn", 1, "kg"), I("Đậu hũ", 500, "g"), I("Nấm hương", 200, "g"), I("Tàu hũ ky", 150, "g"), I("Dứa", 250, "g"), I("Sả", 6, "cây"), I("Củ cải trắng", 400, "g"), I("Rau sống", 400, "g")],
            [S("Nấu nước dùng", "Hầm củ cải, bắp cải, cà rốt và sả với 3 lít nước khoảng 45 phút rồi lọc.", 45, "cook"), S("Chuẩn bị đồ ăn kèm", "Chiên đậu hũ, ngâm nấm và tàu hũ ky; xào nấm với sả băm.", 15, "prep"), S("Nêm nước", "Cho dứa, nấm và tàu hũ ky vào nước dùng; nêm nước tương, muối và sa tế chay.", 15, "cook"), S("Trình bày", "Cho bún và đậu hũ vào tô, chan nước dùng, dùng với rau sống và chanh.", 5, "cook")],
            @"- **Nước dùng rau củ ngọt thanh**: Hầm củ cải trắng, cà rốt, bắp ngọt và lê tươi cùng sả cây đập dập để tạo độ ngọt tự nhiên thanh khiết.
- **Màu sắc và vị cay**: Xào thơm boa-rô băm với dầu màu điều, ớt bột và sả băm rồi trút vào nồi nước dùng để tạo màu sắc hấp dẫn.
- **Topping phong phú**: Ăn kèm chả lụa chay, đậu hũ chiên, nấm rơm và tàu hũ ky chiên giòn rụm cùng đĩa rau sống tươi non."),

        R(
            "ca-ri-chay",
            "Cà ri chay",
            "mon-chay",
            "Cà ri nước cốt dừa béo dịu với đậu hũ, khoai lang, khoai môn, cà rốt và nấm.",
            30,
            40,
            6,
            RecipeDifficulty.Medium,
            N(410, 11, 50, 20, 9, 640),
            [I("Đậu hũ", 500, "g"), I("Khoai lang", 400, "g"), I("Khoai môn", 300, "g"), I("Cà rốt", 250, "g"), I("Nấm", 300, "g"), I("Nước cốt dừa", 500, "ml"), I("Bột cà ri", 2, "muỗng canh"), I("Sả", 4, "cây")],
            [S("Sơ chế", "Cắt rau củ và đậu hũ miếng lớn; chiên sơ khoai và đậu để không vỡ khi nấu.", 20, "prep"), S("Xào gia vị", "Phi sả, thêm bột cà ri và một phần nước cốt dừa, khuấy đến thơm.", 5, "cook"), S("Nấu cà ri", "Thêm rau củ cứng và nước, nấu gần mềm rồi cho nấm, đậu hũ và nước cốt dừa còn lại.", 30, "cook"), S("Hoàn thiện", "Nêm muối, đường và nước tương; dùng nóng với bánh mì.", 5, "cook")],
            @"- **Xào thơm bột cà ri**: Phi thơm boa-rô với sả đập dập và bột cà ri trên lửa nhỏ trước khi cho khoai lang, khoai môn và cà rốt vào đảo săn.
- **Nước cốt dừa béo bùi**: Nấu các loại củ với nước dừa tươi cho chín mềm dẻo, rưới nước cốt dừa đậm đặc ở bước cuối cùng để giữ độ béo thơm.
- **Thưởng thức**: Dọn cà ri nóng sánh vàng cùng bánh mì giòn hoặc bún tươi, rắc ngò rí và vài lát ớt sừng cay nhẹ."),

        R(
            "cha-gio",
            "Chả giò",
            "mon-an-vat",
            "Chả giò vỏ mỏng giòn, nhân thịt tôm, khoai môn, miến và nấm mèo đậm vị.",
            45,
            25,
            6,
            RecipeDifficulty.Medium,
            N(390, 19, 35, 20, 4, 790),
            [I("Bánh tráng chả giò", 30, "miếng"), I("Thịt heo xay", 400, "g"), I("Tôm", 250, "g"), I("Khoai môn", 250, "g"), I("Miến", 60, "g"), I("Nấm mèo", 25, "g"), I("Cà rốt", 120, "g"), I("Trứng gà", 1, "quả")],
            [S("Trộn nhân", "Băm tôm, thái nhỏ miến và nấm; trộn cùng thịt, khoai môn, cà rốt, trứng và gia vị.", 20, "prep"), S("Cuốn", "Đặt lượng nhân vừa phải lên bánh tráng, gấp hai bên và cuốn chắc nhưng không quá chặt.", 20, "cook"), S("Chiên lần một", "Chiên ngập dầu ở 150°C đến chả giò chín và vàng nhạt, vớt để nguội.", 12, "cook"), S("Chiên giòn", "Chiên lần hai ở 180°C đến vàng giòn; dùng với rau sống và nước mắm chua ngọt.", 4, "cook")],
            @"- **Trộn nhân khô ráo**: Thịt nạc vai xay trộn cùng tôm băm, nấm mèo, củ sắn thái sợi vắt kiệt nước để nhân không bị ẩm ướt làm mềm vỏ bánh.
- **Gói chặt vừa phải**: Làm mềm bánh tráng nhẹ nhàng, cuộn đều tay không quá lỏng cũng không quá chặt để khi chiên chả giò không bị bục rách.
- **Chiên hai lần giòn tan**: Chiên lần một ở lửa vừa cho chả chín tới; trước khi ăn chiên lần hai ở dầu nóng già để vỏ ngoài vàng ươm và giòn rụm nhiều giờ."),

        R(
            "che-ba-mau",
            "Chè ba màu",
            "mon-trang-mieng",
            "Chè lạnh ba tầng đậu đỏ, đậu xanh và thạch lá dứa, chan nước cốt dừa.",
            30,
            60,
            6,
            RecipeDifficulty.Medium,
            N(385, 9, 65, 11, 9, 120),
            [I("Đậu đỏ", 200, "g"), I("Đậu xanh cà", 200, "g"), I("Bột rau câu", 10, "g"), I("Nước lá dứa", 300, "ml"), I("Nước cốt dừa", 400, "ml"), I("Đường", 250, "g"), I("Bột năng", 1, "muỗng canh"), I("Đá bào", 600, "g")],
            [S("Nấu đậu đỏ", "Ngâm đậu đỏ 6 giờ, nấu mềm rồi thêm đường vừa ngọt.", 45, "cook"), S("Nấu đậu xanh", "Hấp đậu xanh chín, tán nhuyễn với đường và chút nước cốt dừa.", 25, "cook"), S("Làm thạch", "Nấu bột rau câu với nước lá dứa và đường, đổ khuôn rồi cắt hạt lựu.", 20, "prep"), S("Hoàn thiện", "Xếp thạch, đậu đỏ, đậu xanh vào ly; thêm đá bào và nước cốt dừa đã nấu sánh.", 5, "cook")],
            @"- **Nấu các lớp đậu**: Đậu đỏ và đậu xanh hấp mềm nhuyễn, sên với đường cát cho ngọt dịu vừa ăn; giữ các tầng màu sắc riêng biệt.
- **Lớp thạch lá dứa**: Nấu bột rau câu cùng nước cốt lá dứa tươi tạo màu xanh ngọc bích thơm mát và giòn dai sần sật.
- **Nước cốt dừa sánh béo**: Nấu nước cốt dừa với chút muối, đường và bột năng đến khi sánh mịn; chan lên ly chè cùng đá bào mát lạnh."),

        R(
            "chuoi-nep-nuong",
            "Chuối nếp nướng",
            "mon-trang-mieng",
            "Chuối sứ bọc nếp dẻo nướng thơm, chan nước cốt dừa và rắc đậu phộng mè.",
            35,
            40,
            6,
            RecipeDifficulty.Medium,
            N(405, 7, 68, 13, 5, 150),
            [I("Chuối sứ chín", 8, "quả"), I("Gạo nếp", 500, "g"), I("Nước cốt dừa", 500, "ml"), I("Lá chuối", 8, "miếng"), I("Đậu phộng rang", 80, "g"), I("Mè rang", 30, "g"), I("Đường", 80, "g"), I("Bột năng", 1, "muỗng canh")],
            [S("Đồ nếp", "Ngâm nếp, hấp gần chín rồi trộn với một phần nước cốt dừa, đường và muối.", 30, "cook"), S("Bọc chuối", "Ép nếp thành lớp mỏng, bọc kín từng quả chuối rồi gói lá chuối.", 15, "prep"), S("Nướng", "Nướng trên than hoặc ở 190°C, trở đều đến khi lá cháy thơm và nếp vàng giòn nhẹ.", 25, "cook"), S("Làm sốt", "Nấu nước cốt dừa còn lại với đường, muối và bột năng; chan lên chuối, rắc đậu phộng mè.", 8, "cook")],
            @"- **Đồ nếp dẻo thơm**: Nếp cái hoa vàng ngâm mềm, hấp chín tới rồi trộn cùng nước cốt dừa và đường cho hạt nếp bóng dẻo.
- **Bọc chuối nướng lá**: Dàn mỏng lớp xôi nếp bọc kín quanh trái chuối sứ chín ngọt, quấn lớp lá chuối bên ngoài nướng trên than hoa đến khi nếp xém vàng giòn.
- **Chan nước cốt dừa**: Cắt khúc chuối nướng ra đĩa, chan nước cốt dừa béo ngậy nấu bột báng và rắc đậu phộng rang giòn thơm phức."),
    ];

    private static SeedRecipe[] BuildRecipes() =>
        CoreRecipes.Concat(AdditionalRecipes).Select(EnsureCompleteComposition).ToArray();

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
            steps.Add(S("Nêm lại và hoàn thiện", "Kiểm tra độ chín, nêm lại cho hài hòa rồi trình bày món ăn khi còn ở trạng thái ngon nhất.", 5, "cook"));
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

    private static SeedRecipe R(
        string slug,
        string title,
        string categorySlug,
        string description,
        int prepTime,
        int cookTime,
        int servings,
        RecipeDifficulty difficulty,
        SeedNutrition nutrition,
        IReadOnlyList<SeedIngredient> ingredients,
        IReadOnlyList<SeedStep> steps,
        string? instructions = null) =>
        new(slug, title, categorySlug, description, prepTime, cookTime, servings, difficulty, nutrition, ingredients, steps, instructions);

    private static SeedIngredient I(string name, decimal quantity, string unit, string? notes = null) =>
        new(name, quantity, unit, notes);

    private static SeedStep S(string title, string description, int? timerMinutes = null, string stepType = "cook") =>
        new(title, description, timerMinutes, stepType);

    private static SeedNutrition N(
        decimal calories,
        decimal protein,
        decimal carbohydrates,
        decimal fat,
        decimal fiber,
        decimal sodium) => new(calories, protein, carbohydrates, fat, fiber, sodium);
}
