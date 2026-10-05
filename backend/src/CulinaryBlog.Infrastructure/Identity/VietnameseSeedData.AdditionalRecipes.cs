using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Infrastructure.Identity;

internal static partial class VietnameseSeedData
{
    private static IReadOnlyList<SeedRecipe> AdditionalRecipes { get; } =
    [
        R("pho-ga-ha-noi", "Phở gà Hà Nội", "mon-nuoc", "Nước dùng gà trong, thơm gừng nướng, ăn cùng bánh phở, thịt gà ta và lá chanh.", 35, 90, 4, RecipeDifficulty.Medium, N(445, 32, 55, 12, 3, 870),
            [I("Gà ta", 1.4m, "kg"), I("Bánh phở tươi", 700, "g"), I("Xương gà", 600, "g"), I("Gừng", 60, "g"), I("Hành tây", 1, "củ"), I("Hoa hồi", 3, "cánh"), I("Hành lá", 60, "g"), I("Lá chanh", 8, "lá")],
            [S("Sơ chế gà", "Chà gà với muối, rửa sạch; nướng gừng và hành tây cho dậy mùi.", 15), S("Nấu nước dùng", "Luộc gà cùng xương, gừng, hành và hồi ở lửa nhỏ; hớt bọt để nước trong.", 60), S("Xé thịt gà", "Vớt gà vừa chín, ngâm nước mát rồi lọc xương và thái hoặc xé thịt.", 15), S("Trình bày", "Chần bánh phở, xếp gà, hành và lá chanh, chan nước dùng thật nóng.", 5)]),

        R("bun-mam-mien-tay", "Bún mắm miền Tây", "mon-nuoc", "Nồi bún mắm đậm hương cá linh, có tôm, mực, heo quay và nhiều loại rau miền sông nước.", 45, 75, 6, RecipeDifficulty.Hard, N(590, 38, 64, 21, 7, 1180),
            [I("Mắm cá linh", 250, "g"), I("Bún tươi", 1.2m, "kg"), I("Tôm sú", 400, "g"), I("Mực", 350, "g"), I("Heo quay", 350, "g"), I("Cà tím", 2, "quả"), I("Sả", 6, "cây"), I("Rau đắng và bông súng", 500, "g")],
            [S("Lọc mắm", "Nấu mắm cá với nước đến rã, lọc bỏ xương và xác.", 20), S("Nấu nước dùng", "Hầm sả, hành tím với nước mắm đã lọc, thêm cà tím và nêm vị mặn ngọt.", 35), S("Làm chín hải sản", "Cho tôm, mực vào nước dùng đến vừa chín rồi vớt ra để không dai.", 10), S("Hoàn thiện", "Xếp bún, hải sản và heo quay, chan nước dùng, dùng cùng rau sống miền Tây.", 5)]),

        R("banh-da-cua-hai-phong", "Bánh đa cua Hải Phòng", "mon-nuoc", "Bánh đa đỏ dai trong nước cua đồng, ăn cùng chả lá lốt, tôm, rau muống và hành phi.", 40, 60, 4, RecipeDifficulty.Hard, N(510, 29, 61, 18, 6, 930),
            [I("Cua đồng xay", 500, "g"), I("Bánh đa đỏ", 500, "g"), I("Tôm", 250, "g"), I("Thịt heo xay", 250, "g"), I("Lá lốt", 20, "lá"), I("Rau muống", 300, "g"), I("Cà chua", 3, "quả"), I("Mỡ phần", 100, "g")],
            [S("Lọc và nấu cua", "Lọc cua với nước, đun lửa vừa để riêu đóng mảng rồi vớt riêng.", 20), S("Làm chả lá lốt", "Ướp thịt xay, cuốn lá lốt và áp chảo chín thơm.", 15), S("Nấu nước dùng", "Xào cà chua và gạch cua, cho vào nước cua cùng tôm, nêm vừa vị.", 20), S("Trình bày", "Chần bánh đa và rau muống, thêm riêu, tôm, chả rồi chan nước dùng.", 5)]),

        R("bun-thang-ha-noi", "Bún thang Hà Nội", "mon-nuoc", "Bún thanh nhã với gà xé, trứng tráng, giò lụa thái chỉ và nước dùng tôm khô trong ngọt.", 50, 90, 4, RecipeDifficulty.Expert, N(470, 31, 58, 13, 4, 900),
            [I("Gà ta", 1, "kg"), I("Bún tươi", 700, "g"), I("Trứng gà", 4, "quả"), I("Giò lụa", 250, "g"), I("Tôm khô", 80, "g"), I("Củ cải khô", 60, "g"), I("Nấm hương", 30, "g"), I("Rau răm", 40, "g")],
            [S("Nấu nước dùng", "Luộc gà với tôm khô, nấm hương và hành nướng, hớt bọt kỹ.", 60), S("Chuẩn bị nhân", "Xé gà, thái chỉ giò lụa; tráng trứng thật mỏng rồi thái sợi.", 20), S("Nêm nước", "Lọc nước dùng, thêm củ cải khô đã rửa và nêm nước mắm vừa thanh.", 15), S("Trình bày", "Xếp các loại nhân đều trên bún, chan nước dùng và thêm rau răm.", 5)]),

        R("chao-long", "Cháo lòng", "mon-nuoc", "Cháo gạo rang sánh nhẹ nấu với nước luộc lòng, dùng cùng lòng heo, dồi và rau thơm.", 45, 75, 6, RecipeDifficulty.Hard, N(480, 28, 54, 17, 3, 950),
            [I("Gạo tẻ", 350, "g"), I("Xương heo", 700, "g"), I("Lòng non", 400, "g"), I("Tim heo", 250, "g"), I("Gan heo", 250, "g"), I("Dồi trường", 300, "g"), I("Gừng", 50, "g"), I("Hành lá và rau răm", 100, "g")],
            [S("Làm sạch lòng", "Bóp lòng với muối, giấm và gừng, rửa nhiều lần đến sạch mùi.", 25), S("Luộc lòng", "Luộc riêng từng loại đến vừa chín, ngâm nước mát rồi thái miếng.", 25), S("Nấu cháo", "Rang gạo, ninh với nước xương và nước luộc lòng đến hạt nở sánh.", 60), S("Hoàn thiện", "Nêm cháo, thêm lòng thái, hành, rau răm và tiêu khi dùng.", 5)]),

        R("com-nieu-ca-bong", "Cơm niêu cá bống", "dac-san-vung-mien", "Cơm niêu cháy mỏng ăn với cá bống kho tiêu đậm vị, gợi bữa cơm gia đình miền Trung.", 30, 55, 4, RecipeDifficulty.Medium, N(560, 26, 71, 19, 3, 920),
            [I("Gạo thơm", 500, "g"), I("Cá bống", 600, "g"), I("Nước dừa", 250, "ml"), I("Nước mắm", 3, "muỗng canh"), I("Đường", 1.5m, "muỗng canh"), I("Hành tím", 4, "củ"), I("Ớt", 2, "quả"), I("Tiêu sọ", 1, "thìa cà phê")],
            [S("Nấu cơm niêu", "Vo gạo, cho lượng nước vừa đủ vào niêu và nấu đến khi cơm ráo.", 30), S("Tạo cháy", "Hạ lửa, xoay niêu để đáy cơm tạo lớp cháy vàng mỏng.", 10), S("Kho cá", "Ướp cá rồi kho với nước màu và nước dừa đến khi nước sánh.", 30), S("Hoàn thiện", "Rắc tiêu và ớt lên cá, dùng nóng cùng cơm niêu.", 5)]),

        R("com-am-phu-hue", "Cơm âm phủ Huế", "dac-san-vung-mien", "Đĩa cơm Huế nhiều màu với thịt nướng, chả lụa, tôm, trứng, dưa leo và rau thơm.", 45, 35, 4, RecipeDifficulty.Hard, N(640, 35, 76, 22, 6, 980),
            [I("Gạo", 500, "g"), I("Thịt nạc vai", 350, "g"), I("Tôm", 250, "g"), I("Chả lụa", 200, "g"), I("Trứng gà", 3, "quả"), I("Dưa leo", 2, "quả"), I("Cà rốt", 150, "g"), I("Rau thơm", 100, "g")],
            [S("Nấu cơm", "Nấu cơm hơi khô, xới tơi và để nguội bớt.", 25), S("Chuẩn bị món mặn", "Ướp rồi nướng thịt; luộc tôm, thái chỉ chả lụa.", 25), S("Làm rau và trứng", "Tráng trứng mỏng thái sợi, thái dưa leo và làm cà rốt chua.", 15), S("Trình bày", "Ép cơm giữa đĩa, xếp từng nguyên liệu theo vòng màu và dùng với nước mắm.", 8)]),

        R("xoi-xeo", "Xôi xéo", "com-va-xoi", "Xôi nếp vàng nghệ phủ đậu xanh tán mịn, hành phi giòn và mỡ gà thơm.", 25, 45, 6, RecipeDifficulty.Medium, N(490, 12, 79, 14, 6, 260),
            [I("Gạo nếp", 700, "g"), I("Đậu xanh cà", 300, "g"), I("Bột nghệ", 1, "thìa cà phê"), I("Hành tím", 200, "g"), I("Mỡ gà", 80, "g"), I("Muối", 1.5m, "thìa cà phê"), I("Đường", 1, "muỗng canh"), I("Lá sen", 3, "lá")],
            [S("Ngâm nếp và đậu", "Ngâm riêng nếp với nghệ và đậu xanh ít nhất 6 giờ, để ráo.", 360), S("Đồ xôi", "Trộn nếp với muối rồi hấp đến khi hạt trong dẻo.", 35), S("Làm đậu và hành", "Hấp đậu, giã mịn, nắm chặt; thái hành và phi vàng trong mỡ.", 25), S("Trình bày", "Xới xôi lên lá sen, thái mỏng đậu xanh, rưới mỡ và rắc hành phi.", 5)]),

        R("xoi-ga-la-sen", "Xôi gà lá sen", "com-va-xoi", "Xôi nếp hấp lá sen thơm dịu, trộn thịt gà, nấm hương, lạp xưởng và hạt sen.", 35, 55, 6, RecipeDifficulty.Medium, N(610, 28, 76, 22, 5, 720),
            [I("Gạo nếp", 600, "g"), I("Thịt đùi gà", 450, "g"), I("Hạt sen", 150, "g"), I("Nấm hương", 40, "g"), I("Lạp xưởng", 180, "g"), I("Lá sen", 3, "lá"), I("Nước tương", 2, "muỗng canh"), I("Hành tím", 4, "củ")],
            [S("Đồ xôi", "Ngâm nếp và hấp đến chín khoảng bảy phần.", 30), S("Làm nhân", "Xào gà, nấm, lạp xưởng và hạt sen với hành, nước tương.", 15), S("Gói lá sen", "Trộn nhân với xôi, gói kín trong lá sen đã chần mềm.", 10), S("Hấp hoàn thiện", "Hấp gói xôi đến dẻo và thấm hương lá sen.", 25)]),

        R("com-lam-tay-bac", "Cơm lam Tây Bắc", "dac-san-vung-mien", "Nếp nương nướng trong ống tre non, hạt cơm dẻo thơm mùi tre và lá chuối.", 20, 50, 6, RecipeDifficulty.Medium, N(365, 7, 78, 3, 4, 180),
            [I("Gạo nếp nương", 700, "g"), I("Ống tre non", 6, "ống"), I("Lá chuối", 6, "miếng"), I("Nước suối", 700, "ml"), I("Muối", 1, "thìa cà phê"), I("Mè trắng", 40, "g"), I("Đậu phộng", 60, "g"), I("Dừa nạo", 80, "g")],
            [S("Ngâm nếp", "Vo nhẹ nếp nương, ngâm 4 giờ rồi trộn chút muối.", 240), S("Cho nếp vào ống", "Lót lá chuối, cho nếp và nước vào khoảng hai phần ba ống rồi nút kín.", 10), S("Nướng", "Dựng ống quanh bếp than, xoay đều đến khi vỏ tre cháy sém và nếp chín.", 40), S("Hoàn thiện", "Tước lớp tre ngoài, giữ màng lụa mỏng và dùng với muối mè đậu phộng.", 8)]),

        R("suon-xao-chua-ngot", "Sườn xào chua ngọt", "mon-xao", "Sườn non mềm áo sốt chua ngọt, xào cùng dứa, ớt chuông và hành tây.", 25, 35, 4, RecipeDifficulty.Medium, N(460, 29, 31, 25, 4, 850),
            [I("Sườn non", 800, "g"), I("Dứa", 250, "g"), I("Ớt chuông", 2, "quả"), I("Hành tây", 1, "củ"), I("Cà chua", 2, "quả"), I("Giấm gạo", 2, "muỗng canh"), I("Đường", 2, "muỗng canh"), I("Bột năng", 1, "muỗng canh")],
            [S("Sơ chế sườn", "Chần sườn, để ráo rồi ướp nước mắm, tiêu và chút bột năng.", 20), S("Chiên sườn", "Áp chảo sườn vàng đều, thêm ít nước và om đến mềm.", 20), S("Pha sốt", "Khuấy giấm, đường, nước mắm và nước, nếm vị chua ngọt cân bằng.", 5), S("Xào hoàn thiện", "Xào rau củ lửa lớn, thêm sườn và sốt, đảo đến khi bám đều.", 8)]),

        R("mam-kho-quet", "Mắm kho quẹt", "mon-kho-va-rim", "Mắm kho quẹt sánh mặn ngọt với tóp mỡ và tôm khô, dùng chấm rau củ luộc.", 20, 25, 4, RecipeDifficulty.Easy, N(310, 13, 20, 21, 2, 1250),
            [I("Thịt ba chỉ", 300, "g"), I("Tôm khô", 80, "g"), I("Nước mắm", 6, "muỗng canh"), I("Đường thốt nốt", 3, "muỗng canh"), I("Hành tím", 4, "củ"), I("Tỏi", 4, "tép"), I("Tiêu xanh", 3, "nhánh"), I("Rau củ luộc", 800, "g")],
            [S("Làm tóp mỡ", "Thái ba chỉ hạt lựu, thắng lửa vừa đến vàng giòn và chắt bớt mỡ.", 12), S("Phi thơm", "Dùng ít mỡ phi hành tỏi, cho tôm khô đã ngâm vào đảo thơm.", 5), S("Kho mắm", "Thêm nước mắm, đường và nước; kho nhỏ lửa đến sánh.", 12), S("Hoàn thiện", "Cho tóp mỡ, tiêu xanh và ớt vào, dùng nóng với rau củ luộc.", 3)]),

        R("ca-nuc-kho-thom", "Cá nục kho thơm", "mon-kho-va-rim", "Cá nục chắc thịt kho cùng dứa chua ngọt, nước dừa và tiêu đến thấm mềm.", 20, 50, 4, RecipeDifficulty.Easy, N(345, 30, 18, 17, 2, 910),
            [I("Cá nục", 900, "g"), I("Dứa", 350, "g"), I("Nước dừa", 350, "ml"), I("Nước mắm", 3, "muỗng canh"), I("Đường", 1.5m, "muỗng canh"), I("Hành tím", 4, "củ"), I("Ớt", 2, "quả"), I("Tiêu", 1, "thìa cà phê")],
            [S("Sơ chế cá", "Làm sạch cá, cắt khúc và ướp nước mắm, hành, tiêu.", 20), S("Xếp nồi", "Lót dứa dưới đáy, xếp cá lên và thêm phần dứa còn lại.", 5), S("Kho cá", "Thêm nước màu và nước dừa, kho lửa nhỏ đến cá thấm, nước sánh.", 40), S("Hoàn thiện", "Nêm lại, thêm ớt và tiêu, để cá nghỉ trước khi dùng với cơm.", 5)]),

        R("ga-rang-sa-ot", "Gà rang sả ớt", "mon-kho-va-rim", "Gà ta rang săn với sả băm, ớt và nước mắm, thơm cay và rất đưa cơm.", 20, 30, 4, RecipeDifficulty.Easy, N(390, 36, 11, 22, 2, 820),
            [I("Gà ta", 900, "g"), I("Sả", 6, "cây"), I("Ớt", 4, "quả"), I("Nước mắm", 3, "muỗng canh"), I("Đường", 1, "muỗng canh"), I("Hành tím", 4, "củ"), I("Tỏi", 4, "tép"), I("Nghệ tươi", 20, "g")],
            [S("Ướp gà", "Chặt gà miếng vừa, ướp nước mắm, nghệ, hành tỏi và một nửa sả.", 20), S("Phi sả", "Phi phần sả còn lại đến vàng thơm, vớt một nửa để riêng.", 6), S("Rang gà", "Cho gà vào đảo lửa vừa đến săn, thêm ít nước và đậy nắp cho chín.", 20), S("Hoàn thiện", "Mở nắp rang cạn, thêm ớt và sả phi để món khô thơm.", 5)]),

        R("thit-dong-mien-bac", "Thịt đông miền Bắc", "mon-ham", "Thịt chân giò và bì ninh mềm cùng nấm mèo, tiêu sọ, để lạnh thành khối trong dịp Tết.", 35, 90, 8, RecipeDifficulty.Medium, N(360, 28, 7, 24, 2, 720),
            [I("Thịt chân giò", 900, "g"), I("Bì heo", 250, "g"), I("Nấm mèo", 40, "g"), I("Nấm hương", 30, "g"), I("Cà rốt", 1, "củ"), I("Hành tím", 4, "củ"), I("Nước mắm", 2, "muỗng canh"), I("Tiêu sọ", 1, "thìa cà phê")],
            [S("Sơ chế", "Chần thịt và bì, rửa sạch; ngâm nấm rồi thái sợi.", 20), S("Xào thịt", "Phi hành, xào thịt săn và nêm nước mắm, tiêu.", 10), S("Ninh", "Thêm nước vừa ngập, ninh nhỏ lửa và hớt bọt đến thịt mềm.", 70), S("Đổ khuôn", "Cho nấm vào cuối, xếp cà rốt dưới khuôn, múc thịt và nước rồi để lạnh.", 240)]),

        R("dau-co-ve-xao-bo", "Đậu cô ve xào bò", "mon-xao", "Đậu cô ve xanh giòn xào nhanh với thịt bò mềm, tỏi và tiêu.", 20, 12, 4, RecipeDifficulty.Easy, N(285, 27, 18, 13, 6, 620),
            [I("Đậu cô ve", 500, "g"), I("Thịt bò thăn", 350, "g"), I("Tỏi", 5, "tép"), I("Dầu hào", 1.5m, "muỗng canh"), I("Nước tương", 1, "muỗng canh"), I("Bột năng", 1, "thìa cà phê"), I("Dầu mè", 1, "thìa cà phê"), I("Tiêu", 0.5m, "thìa cà phê")],
            [S("Ướp bò", "Thái bò mỏng, ướp dầu hào, nước tương, bột năng và dầu mè.", 15), S("Chần đậu", "Tước xơ đậu, chần nhanh trong nước sôi có muối rồi làm nguội.", 3), S("Xào bò", "Phi tỏi, xào bò lửa lớn đến tái rồi trút ra.", 3), S("Xào hoàn thiện", "Xào đậu chín giòn, cho bò lại chảo, đảo nhanh và rắc tiêu.", 4)]),

        R("long-ga-xao-muop", "Lòng gà xào mướp", "mon-xao", "Lòng gà làm sạch xào mướp hương vừa chín tới, thơm hành răm và tiêu.", 25, 12, 4, RecipeDifficulty.Medium, N(250, 22, 14, 12, 4, 690),
            [I("Lòng gà", 450, "g"), I("Mướp hương", 2, "quả"), I("Giá đỗ", 200, "g"), I("Hành tím", 3, "củ"), I("Tỏi", 3, "tép"), I("Nước mắm", 1.5m, "muỗng canh"), I("Rau răm", 40, "g"), I("Tiêu", 0.5m, "thìa cà phê")],
            [S("Sơ chế lòng", "Bóp lòng gà với muối gừng, rửa sạch, thái vừa ăn và ướp gia vị.", 15), S("Xào lòng", "Phi hành tỏi, xào lòng lửa lớn đến vừa chín rồi trút ra.", 5), S("Xào mướp", "Cho mướp vào chảo, đảo nhanh để tiết nước tự nhiên, thêm giá.", 4), S("Hoàn thiện", "Cho lòng lại chảo, nêm nước mắm, thêm rau răm và tiêu.", 3)]),

        R("bong-thien-ly-xao-bo", "Bông thiên lý xào bò", "mon-xao", "Bông thiên lý giòn ngọt xào thịt bò mềm, món rau mùa hè thanh và thơm.", 20, 10, 4, RecipeDifficulty.Easy, N(265, 26, 13, 13, 5, 590),
            [I("Bông thiên lý", 450, "g"), I("Thịt bò", 350, "g"), I("Tỏi", 5, "tép"), I("Dầu hào", 1, "muỗng canh"), I("Nước mắm", 1, "muỗng canh"), I("Bột năng", 1, "thìa cà phê"), I("Dầu ăn", 2, "muỗng canh"), I("Tiêu", 0.5m, "thìa cà phê")],
            [S("Ướp bò", "Thái bò ngang thớ, ướp dầu hào, nước mắm, tiêu và bột năng.", 15), S("Sơ chế hoa", "Nhặt cuống già, rửa nhẹ bông thiên lý và để thật ráo.", 8), S("Xào bò", "Phi tỏi, xào bò lửa lớn đến tái rồi lấy ra.", 3), S("Hoàn thiện", "Xào hoa vừa chín giòn, cho bò lại chảo và đảo nhanh.", 3)]),

        R("mien-xao-long-ga", "Miến xào lòng gà", "mon-xao", "Miến dong dai mềm xào lòng gà, nấm hương, nấm mèo và rau củ.", 30, 18, 4, RecipeDifficulty.Medium, N(420, 24, 55, 12, 6, 760),
            [I("Miến dong", 350, "g"), I("Lòng gà", 400, "g"), I("Nấm hương", 30, "g"), I("Nấm mèo", 25, "g"), I("Cà rốt", 120, "g"), I("Cải ngọt", 250, "g"), I("Nước tương", 2, "muỗng canh"), I("Hành tím", 3, "củ")],
            [S("Chuẩn bị miến", "Ngâm miến vừa mềm, cắt ngắn và trộn chút dầu để không dính.", 15), S("Xào lòng", "Làm sạch lòng, thái miếng rồi xào với hành đến chín.", 6), S("Xào rau", "Xào nấm, cà rốt và cải trên lửa lớn, nêm nhẹ.", 5), S("Hoàn thiện", "Thêm miến và lòng, nêm nước tương rồi đảo đến miến thấm và tơi.", 5)]),

        R("su-su-xao-toi", "Su su xào tỏi", "mon-xao", "Su su thái sợi xào tỏi lửa lớn, giữ độ xanh giòn và vị ngọt mát.", 15, 8, 4, RecipeDifficulty.Easy, N(145, 4, 18, 7, 5, 420),
            [I("Su su", 3, "quả"), I("Tỏi", 6, "tép"), I("Dầu ăn", 2, "muỗng canh"), I("Nước mắm", 1, "muỗng canh"), I("Muối", 0.5m, "thìa cà phê"), I("Hành lá", 30, "g"), I("Rau mùi", 20, "g"), I("Tiêu", 0.25m, "thìa cà phê")],
            [S("Sơ chế", "Gọt su su dưới vòi nước, bỏ lõi và thái sợi đều.", 10), S("Phi tỏi", "Đập dập tỏi, phi một nửa đến vàng rồi để riêng.", 3), S("Xào", "Cho su su vào chảo thật nóng, đảo nhanh và nêm nước mắm, muối.", 5), S("Hoàn thiện", "Tắt bếp khi su su còn giòn, thêm tỏi phi, hành và tiêu.", 2)]),

        R("canh-ca-doc-mung", "Canh cá nấu dọc mùng", "canh-va-sup", "Canh cá chua dịu với dọc mùng giòn, cà chua, me và rau ngổ.", 30, 35, 4, RecipeDifficulty.Medium, N(245, 27, 14, 9, 5, 680),
            [I("Cá lóc", 700, "g"), I("Dọc mùng", 400, "g"), I("Cà chua", 3, "quả"), I("Me chín", 60, "g"), I("Dứa", 200, "g"), I("Đậu bắp", 150, "g"), I("Rau ngổ", 40, "g"), I("Nghệ", 15, "g")],
            [S("Sơ chế", "Làm sạch cá; tước dọc mùng, bóp muối và rửa nhiều lần.", 20), S("Nấu nền canh", "Xào cà chua với nghệ, thêm nước và nước me rồi đun sôi.", 10), S("Nấu cá", "Cho cá vào nấu lửa vừa đến chín, hớt bọt nhẹ.", 15), S("Hoàn thiện", "Thêm dọc mùng, dứa, đậu bắp; nêm vị và rắc rau ngổ.", 8)]),

        R("canh-bau-nau-tom", "Canh bầu nấu tôm", "canh-va-sup", "Canh bầu thanh ngọt nấu tôm tươi giã dập, điểm hành lá và tiêu.", 15, 15, 4, RecipeDifficulty.Easy, N(150, 18, 12, 4, 4, 520),
            [I("Bầu", 700, "g"), I("Tôm tươi", 300, "g"), I("Hành tím", 2, "củ"), I("Nước mắm", 1, "muỗng canh"), I("Hành lá", 40, "g"), I("Rau mùi", 20, "g"), I("Muối", 0.5m, "thìa cà phê"), I("Tiêu", 0.25m, "thìa cà phê")],
            [S("Sơ chế", "Gọt bầu, thái miếng; bóc tôm, giã dập cùng hành tím.", 10), S("Xào tôm", "Phi hành, xào tôm đến chuyển màu và thơm.", 3), S("Nấu canh", "Thêm nước, đun sôi rồi cho bầu vào nấu vừa trong.", 7), S("Hoàn thiện", "Nêm nước mắm, tắt bếp và thêm hành, rau mùi, tiêu.", 2)]),

        R("canh-mang-mong-gio", "Canh măng móng giò", "canh-va-sup", "Móng giò hầm mềm cùng măng khô, món canh đậm vị thường có trong mâm cỗ Tết.", 45, 100, 6, RecipeDifficulty.Hard, N(420, 29, 18, 27, 6, 820),
            [I("Móng giò", 1.2m, "kg"), I("Măng khô", 300, "g"), I("Xương heo", 500, "g"), I("Hành tím", 4, "củ"), I("Nước mắm", 2, "muỗng canh"), I("Hành lá", 60, "g"), I("Rau mùi", 40, "g"), I("Tiêu", 0.5m, "thìa cà phê")],
            [S("Ngâm măng", "Ngâm măng qua đêm, luộc thay nước nhiều lần rồi xé miếng.", 480), S("Sơ chế giò", "Chần móng giò và xương, rửa sạch để nước canh trong.", 15), S("Hầm", "Hầm xương và móng giò lửa nhỏ đến gần mềm, thường xuyên hớt bọt.", 75), S("Nấu măng", "Xào măng với nước mắm, cho vào nồi hầm thêm đến thấm, thêm hành mùi.", 25)]),

        R("sup-cua", "Súp cua", "canh-va-sup", "Súp sánh trong với thịt cua, gà xé, nấm tuyết, bắp và trứng tạo vân.", 30, 35, 6, RecipeDifficulty.Medium, N(240, 23, 25, 6, 3, 760),
            [I("Thịt cua", 300, "g"), I("Ức gà", 250, "g"), I("Bắp ngọt", 200, "g"), I("Nấm tuyết", 40, "g"), I("Trứng gà", 2, "quả"), I("Trứng cút", 12, "quả"), I("Bột năng", 4, "muỗng canh"), I("Nước dùng gà", 1.5m, "lít")],
            [S("Chuẩn bị", "Luộc gà và trứng cút; xé gà, ngâm nấm tuyết và tách nhỏ.", 20), S("Nấu súp", "Đun nước dùng với bắp, gà, cua và nấm đến các nguyên liệu chín.", 15), S("Tạo độ sánh", "Rót bột năng pha loãng từ từ, khuấy đến độ sánh vừa.", 5), S("Tạo vân trứng", "Rót trứng đánh theo dòng mảnh, khuấy nhẹ; thêm trứng cút và tiêu.", 4)]),

        R("chao-ga", "Cháo gà", "canh-va-sup", "Cháo gạo rang nấu nước luộc gà, thịt gà xé mềm và nhiều hành rau răm.", 25, 65, 6, RecipeDifficulty.Easy, N(390, 28, 48, 10, 3, 650),
            [I("Gà ta", 1.2m, "kg"), I("Gạo tẻ", 300, "g"), I("Gạo nếp", 80, "g"), I("Gừng", 40, "g"), I("Hành tím", 4, "củ"), I("Nấm rơm", 200, "g"), I("Hành lá", 60, "g"), I("Rau răm", 50, "g")],
            [S("Luộc gà", "Luộc gà với gừng, hành và muối đến vừa chín, vớt ra để nguội.", 35), S("Nấu cháo", "Rang hai loại gạo, cho vào nước luộc gà ninh đến nở mềm.", 45), S("Chuẩn bị thịt", "Xé thịt gà, xào nấm rơm và lọc bỏ xương khỏi nước dùng.", 15), S("Hoàn thiện", "Cho nấm vào cháo, nêm nước mắm; thêm gà xé, hành và rau răm.", 5)]),

        R("goi-bo-bop-thau", "Gỏi bò bóp thấu", "mon-cuon-va-goi", "Thịt bò tái mềm trộn khế, chuối chát, hành tây, rau thơm và nước mắm chua ngọt.", 35, 10, 4, RecipeDifficulty.Medium, N(320, 28, 21, 14, 6, 720),
            [I("Thịt bò thăn", 500, "g"), I("Khế chua", 2, "quả"), I("Chuối chát", 2, "quả"), I("Hành tây", 1, "củ"), I("Cà rốt", 120, "g"), I("Rau thơm", 120, "g"), I("Đậu phộng", 80, "g"), I("Chanh", 3, "quả")],
            [S("Ướp bò", "Thái bò thật mỏng, ướp chút tỏi, nước mắm và tiêu.", 15), S("Sơ chế rau", "Thái khế, chuối, hành và cà rốt; ngâm hành trong nước đá.", 15), S("Làm tái bò", "Xào hoặc chần bò thật nhanh để thịt vừa tái.", 2), S("Trộn gỏi", "Bóp nhẹ bò với nước trộn, thêm rau củ và rau thơm, rắc đậu phộng.", 5)]),

        R("goi-du-du-kho-bo", "Gỏi đu đủ khô bò", "mon-cuon-va-goi", "Đu đủ xanh giòn trộn khô bò, rau răm, đậu phộng và nước tương chua cay.", 25, 5, 4, RecipeDifficulty.Easy, N(260, 15, 34, 8, 7, 760),
            [I("Đu đủ xanh", 600, "g"), I("Khô bò", 200, "g"), I("Cà rốt", 120, "g"), I("Rau răm", 60, "g"), I("Đậu phộng", 80, "g"), I("Nước tương", 3, "muỗng canh"), I("Chanh", 2, "quả"), I("Ớt sa tế", 1, "muỗng canh")],
            [S("Bào đu đủ", "Bào sợi đu đủ và cà rốt, ngâm nước đá rồi để thật ráo.", 15), S("Pha nước trộn", "Khuấy nước tương, chanh, đường, tỏi và sa tế.", 5), S("Chuẩn bị khô bò", "Xé khô bò sợi vừa ăn, rang đậu phộng và giã dập.", 5), S("Trộn gỏi", "Trộn đu đủ với nước sốt, thêm rau răm, khô bò và đậu phộng.", 3)]),

        R("goi-ca-mai", "Gỏi cá mai", "hai-san", "Cá mai tươi làm tái bằng chanh, cuốn bánh tráng với rau và nước chấm đậu phộng.", 45, 5, 4, RecipeDifficulty.Hard, N(300, 30, 24, 10, 5, 640),
            [I("Cá mai tươi", 600, "g"), I("Chanh", 6, "quả"), I("Hành tây", 1, "củ"), I("Gừng", 40, "g"), I("Đậu phộng", 100, "g"), I("Bánh tráng", 20, "miếng"), I("Chuối chát", 2, "quả"), I("Rau sống", 400, "g")],
            [S("Làm cá", "Đánh vảy, bỏ đầu và xương, rửa cá bằng nước lạnh rồi thấm khô.", 25), S("Làm tái", "Trộn cá với nước cốt chanh đến thịt chuyển trắng, vắt ráo nhẹ.", 8), S("Trộn gỏi", "Trộn cá với hành tây, gừng, rau thơm và đậu phộng.", 5), S("Pha nước chấm", "Nấu sốt đậu phộng chua ngọt, dùng gỏi cuốn bánh tráng và rau.", 8)]),

        R("cuon-diep", "Cuốn diếp", "mon-cuon-va-goi", "Lá cải xanh cuốn bún, tôm, thịt, trứng và rau thơm, dùng cùng tương đậu.", 40, 20, 4, RecipeDifficulty.Medium, N(350, 24, 39, 11, 6, 610),
            [I("Cải bẹ xanh", 16, "lá"), I("Tôm", 300, "g"), I("Thịt ba chỉ", 300, "g"), I("Bún tươi", 400, "g"), I("Trứng gà", 3, "quả"), I("Cà rốt", 120, "g"), I("Dưa leo", 2, "quả"), I("Rau thơm", 100, "g")],
            [S("Chuẩn bị lá", "Chọn lá cải non, rửa sạch và chần rất nhanh cho mềm.", 8), S("Làm nhân", "Luộc tôm thịt, tráng trứng và thái tất cả thành thanh dài.", 20), S("Cuốn", "Trải lá cải, đặt bún, rau và các loại nhân rồi cuốn chắc tay.", 15), S("Làm tương", "Nấu tương đậu với tỏi, đường và đậu phộng đến sánh để chấm.", 8)]),

        R("nem-lui-hue", "Nem lụi Huế", "dac-san-vung-mien", "Thịt heo quết dẻo bọc que sả nướng, cuốn bánh tráng và chấm sốt gan đậu phộng.", 45, 25, 4, RecipeDifficulty.Hard, N(520, 30, 42, 25, 5, 850),
            [I("Thịt nạc vai", 600, "g"), I("Giò sống", 250, "g"), I("Sả", 12, "cây"), I("Bánh tráng", 20, "miếng"), I("Gan heo", 150, "g"), I("Đậu phộng", 100, "g"), I("Dứa", 200, "g"), I("Rau sống", 400, "g")],
            [S("Quết thịt", "Xay thịt lạnh, trộn giò sống và gia vị rồi quết đến dẻo.", 20), S("Tạo nem", "Bọc thịt quanh phần gốc que sả thành lớp đều.", 15), S("Nướng", "Nướng nem trên than lửa vừa, trở đều đến vàng thơm.", 15), S("Làm nước lèo", "Xào gan xay, thêm tương, nước và đậu phộng; nấu sánh để chấm nem cuốn.", 12)]),

        R("banh-chung", "Bánh chưng", "banh-viet", "Bánh nếp vuông gói lá dong, nhân đậu xanh và thịt ba chỉ, luộc chín dền dịp Tết.", 120, 600, 8, RecipeDifficulty.Expert, N(570, 20, 80, 19, 7, 680),
            [I("Gạo nếp", 1.5m, "kg"), I("Đậu xanh cà", 600, "g"), I("Thịt ba chỉ", 700, "g"), I("Lá dong", 20, "lá"), I("Lạt giang", 8, "sợi"), I("Hành tím", 6, "củ"), I("Muối", 3, "thìa cà phê"), I("Tiêu", 1, "thìa cà phê")],
            [S("Chuẩn bị", "Ngâm nếp và đậu riêng; ướp thịt với hành, nước mắm và tiêu.", 360), S("Làm nhân", "Hấp đậu vừa chín, giã mịn và bọc quanh miếng thịt.", 30), S("Gói bánh", "Xếp lá vào khuôn, cho nếp, nhân, phủ nếp rồi gói vuông và buộc lạt.", 30), S("Luộc bánh", "Luộc ngập nước 9–10 giờ, châm nước sôi thường xuyên; ép bánh khi nguội.", 600)]),

        R("banh-tet-nhan-man", "Bánh tét nhân mặn", "banh-viet", "Đòn bánh tét Nam Bộ dẻo nếp, nhân đậu xanh thịt mỡ, gói lá chuối và nấu lâu.", 100, 540, 10, RecipeDifficulty.Expert, N(585, 19, 82, 20, 7, 700),
            [I("Gạo nếp", 1.5m, "kg"), I("Đậu xanh cà", 600, "g"), I("Thịt ba chỉ", 700, "g"), I("Lá chuối", 20, "lá"), I("Dây lạt", 12, "sợi"), I("Nước cốt dừa", 250, "ml"), I("Hành tím", 5, "củ"), I("Tiêu", 1, "thìa cà phê")],
            [S("Chuẩn bị nếp", "Ngâm nếp, để ráo rồi xào nhanh với nước cốt dừa và muối.", 360), S("Làm nhân", "Hấp đậu, tán mịn; ướp thịt ba chỉ với hành, nước mắm và tiêu.", 30), S("Gói đòn", "Trải lá chuối, dàn nếp, đậu và thịt; cuộn chặt thành đòn và buộc đều.", 35), S("Nấu bánh", "Nấu bánh ngập nước 8–9 giờ, vớt rửa sạch và treo ráo.", 540)]),

        R("banh-gio", "Bánh giò", "banh-viet", "Bánh bột gạo mềm hình chóp, nhân thịt băm nấm mèo thơm tiêu, gói lá chuối.", 50, 35, 8, RecipeDifficulty.Hard, N(390, 16, 52, 13, 3, 650),
            [I("Bột gạo", 400, "g"), I("Bột năng", 80, "g"), I("Thịt heo xay", 400, "g"), I("Nấm mèo", 30, "g"), I("Hành tím", 6, "củ"), I("Nước dùng xương", 1.5m, "lít"), I("Lá chuối", 12, "miếng"), I("Tiêu", 0.5m, "thìa cà phê")],
            [S("Xào nhân", "Xào thịt với hành tím, nấm mèo, nước mắm và tiêu đến tơi.", 12), S("Khuấy bột", "Hòa bột với nước dùng, khuấy lửa nhỏ đến đặc mịn và nửa chín.", 15), S("Gói bánh", "Gấp lá thành phễu, cho lớp bột, nhân và phủ kín bằng bột.", 20), S("Hấp", "Hấp bánh 25–30 phút, để nghỉ vài phút trước khi dùng.", 30)]),

        R("banh-duc-nong", "Bánh đúc nóng", "banh-viet", "Bánh đúc gạo mềm nóng, chan thịt băm nấm mèo, nước mắm và hành phi.", 35, 35, 6, RecipeDifficulty.Medium, N(410, 18, 55, 14, 3, 780),
            [I("Bột gạo", 350, "g"), I("Bột năng", 80, "g"), I("Thịt heo xay", 350, "g"), I("Nấm mèo", 30, "g"), I("Hành tím", 8, "củ"), I("Nước mắm", 4, "muỗng canh"), I("Giấm gạo", 2, "muỗng canh"), I("Rau mùi", 50, "g")],
            [S("Ngâm bột", "Hòa hai loại bột với nước và muối, để nghỉ rồi gạn thay nước.", 120), S("Khuấy bánh", "Khuấy bột trên lửa nhỏ liên tục đến trong, dẻo và mịn.", 25), S("Xào nhân", "Xào thịt, nấm mèo với hành và nước mắm đến tơi thơm.", 10), S("Hoàn thiện", "Múc bánh nóng, thêm nhân, nước mắm chua ngọt, hành phi và rau mùi.", 5)]),

        R("banh-tam-bi", "Bánh tằm bì", "dac-san-vung-mien", "Sợi bánh tằm mềm dùng với bì thịt, rau sống, nước cốt dừa và nước mắm chua ngọt.", 40, 35, 6, RecipeDifficulty.Hard, N(530, 21, 66, 21, 6, 850),
            [I("Bột gạo", 400, "g"), I("Bột năng", 150, "g"), I("Thịt nạc", 300, "g"), I("Bì heo", 200, "g"), I("Thính gạo", 40, "g"), I("Nước cốt dừa", 350, "ml"), I("Giá đỗ", 250, "g"), I("Rau sống", 300, "g")],
            [S("Làm bánh tằm", "Nhồi bột với nước nóng, se sợi ngắn rồi luộc đến nổi và xả mát.", 25), S("Làm bì", "Luộc thịt và bì, thái sợi rồi trộn với thính rang.", 20), S("Nấu nước cốt", "Nấu nước cốt dừa với muối, đường và chút bột năng đến sánh.", 8), S("Trình bày", "Xếp rau, giá, bánh tằm và bì; chan nước cốt dừa và nước mắm.", 5)]),

        R("thit-xien-nuong", "Thịt xiên nướng", "mon-nuong", "Thịt vai ướp sả, mật ong và nước mắm, xiên que nướng vàng xém kiểu hàng quà.", 35, 20, 4, RecipeDifficulty.Easy, N(410, 30, 20, 23, 2, 790),
            [I("Thịt vai heo", 800, "g"), I("Sả", 5, "cây"), I("Hành tím", 5, "củ"), I("Tỏi", 5, "tép"), I("Nước mắm", 3, "muỗng canh"), I("Mật ong", 2, "muỗng canh"), I("Dầu hào", 1, "muỗng canh"), I("Que tre", 16, "que")],
            [S("Thái thịt", "Thái thịt lát mỏng vừa, giữ một phần mỡ để xiên không khô.", 12), S("Ướp", "Ướp thịt với sả, hành tỏi, nước mắm, mật ong và dầu hào.", 120), S("Xiên", "Ngâm que tre, xiên thịt gấp nếp lỏng tay để chín đều.", 15), S("Nướng", "Nướng than lửa vừa, trở và quét nước ướp đến vàng xém.", 15)]),

        R("bo-nuong-la-lot", "Bò nướng lá lốt", "mon-nuong", "Thịt bò băm cuốn lá lốt nướng thơm, dùng với bún, rau và mắm nêm.", 40, 18, 4, RecipeDifficulty.Medium, N(440, 30, 29, 23, 5, 820),
            [I("Thịt bò xay", 600, "g"), I("Mỡ heo", 100, "g"), I("Lá lốt", 40, "lá"), I("Sả", 3, "cây"), I("Hành tím", 4, "củ"), I("Bún tươi", 500, "g"), I("Bánh tráng", 16, "miếng"), I("Rau sống", 350, "g")],
            [S("Trộn nhân", "Trộn bò, mỡ băm với sả, hành, nước mắm và tiêu; để thấm.", 20), S("Cuốn", "Đặt nhân lên mặt trong lá lốt, cuốn chặt và ghim cuống.", 20), S("Nướng", "Nướng trên than lửa vừa, quét dầu đến lá xém thơm và nhân chín.", 10), S("Trình bày", "Dùng cuốn bánh tráng với bún, rau, đồ chua và mắm nêm.", 5)]),

        R("vit-nuong-chao", "Vịt nướng chao", "mon-nuong", "Vịt ướp chao đỏ, sa tế và ngũ vị, nướng da vàng thơm, thịt đậm vị.", 45, 55, 6, RecipeDifficulty.Hard, N(510, 38, 16, 32, 2, 980),
            [I("Vịt", 1.8m, "kg"), I("Chao đỏ", 5, "viên"), I("Sả", 5, "cây"), I("Tỏi", 6, "tép"), I("Sa tế", 2, "muỗng canh"), I("Mật ong", 2, "muỗng canh"), I("Ngũ vị hương", 1, "thìa cà phê"), I("Gừng", 50, "g")],
            [S("Khử mùi", "Chà vịt với gừng và rượu, rửa sạch rồi thấm khô.", 15), S("Ướp vịt", "Tán chao, trộn sả tỏi, sa tế, mật ong, ngũ vị rồi xoa đều vịt.", 180), S("Nướng", "Nướng vịt ở 180°C, trở và quét sốt định kỳ đến chín vàng.", 50), S("Hoàn thiện", "Để vịt nghỉ trước khi chặt, dùng với rau răm và chao pha.", 10)]),

        R("suon-nuong-mat-ong", "Sườn nướng mật ong", "mon-nuong", "Sườn non nướng mềm, lớp ngoài óng vàng từ mật ong, tỏi và nước mắm.", 30, 40, 4, RecipeDifficulty.Medium, N(490, 32, 24, 29, 2, 860),
            [I("Sườn non", 1, "kg"), I("Mật ong", 3, "muỗng canh"), I("Nước mắm", 3, "muỗng canh"), I("Tỏi", 6, "tép"), I("Hành tím", 4, "củ"), I("Dầu hào", 1, "muỗng canh"), I("Dầu điều", 1, "muỗng canh"), I("Ngũ vị hương", 0.5m, "thìa cà phê")],
            [S("Sơ chế", "Chặt sườn thanh vừa, ngâm nước lạnh rồi thấm khô.", 15), S("Ướp", "Trộn gia vị, giữ lại ít mật ong; ướp sườn ít nhất 2 giờ.", 120), S("Nướng chín", "Nướng 175°C đến sườn chín mềm, trở mặt giữa thời gian.", 30), S("Tạo màu", "Quét mật ong pha dầu, tăng nhiệt nướng nhanh đến mặt sườn óng vàng.", 8)]),

        R("tom-nuong-muoi-ot", "Tôm nướng muối ớt", "hai-san", "Tôm sú nguyên vỏ ướp muối ớt, nướng than đến đỏ au, thịt ngọt và săn.", 20, 12, 4, RecipeDifficulty.Easy, N(260, 32, 8, 11, 2, 720),
            [I("Tôm sú", 800, "g"), I("Ớt sừng", 4, "quả"), I("Muối hột", 1, "muỗng canh"), I("Tỏi", 4, "tép"), I("Dầu điều", 1, "muỗng canh"), I("Mật ong", 1, "muỗng canh"), I("Chanh", 2, "quả"), I("Rau răm", 80, "g")],
            [S("Sơ chế tôm", "Cắt râu, rút chỉ lưng nhưng giữ nguyên vỏ, rửa và thấm khô.", 10), S("Làm sốt", "Giã ớt, tỏi, muối rồi trộn dầu điều và mật ong.", 5), S("Ướp", "Xoa sốt lên tôm và để thấm trong 15 phút.", 15), S("Nướng", "Nướng than lửa vừa, trở đều đến vỏ đỏ và thịt vừa chín.", 8)]),

        R("nem-chua-ran", "Nem chua rán", "mon-chien", "Nem thịt chua nhẹ áo bột chiên xù, vỏ vàng giòn, bên trong dẻo mềm.", 25, 15, 4, RecipeDifficulty.Medium, N(430, 23, 35, 22, 3, 870),
            [I("Nem chua sống", 500, "g"), I("Bột mì", 100, "g"), I("Bột chiên xù", 180, "g"), I("Trứng gà", 2, "quả"), I("Dầu ăn", 600, "ml"), I("Tương ớt", 100, "ml"), I("Dưa leo", 2, "quả"), I("Xoài xanh", 1, "quả")],
            [S("Làm lạnh nem", "Bóc lá, để nem trong ngăn mát giúp giữ dáng khi áo bột.", 30), S("Áo bột", "Lăn nem qua bột mì, trứng đánh rồi bột chiên xù.", 12), S("Chiên", "Chiên ngập dầu 170°C đến vàng đều, không chiên quá lâu.", 6), S("Hoàn thiện", "Để ráo dầu, dùng nóng với tương ớt, dưa leo và xoài xanh.", 3)]),

        R("bot-chien", "Bột chiên", "mon-chien", "Khối bột gạo chiên vàng cạnh cùng trứng, hành lá, ăn với đồ chua và nước tương.", 40, 20, 4, RecipeDifficulty.Medium, N(510, 16, 70, 19, 4, 880),
            [I("Bột gạo", 400, "g"), I("Bột năng", 80, "g"), I("Trứng gà", 4, "quả"), I("Đu đủ xanh", 200, "g"), I("Hành lá", 60, "g"), I("Nước tương", 4, "muỗng canh"), I("Giấm gạo", 2, "muỗng canh"), I("Dầu ăn", 100, "ml")],
            [S("Khuấy bột", "Hòa bột với nước và muối, khuấy lửa nhỏ đến đặc.", 15), S("Hấp bột", "Đổ bột vào khuôn, hấp chín, để nguội hẳn rồi cắt khối.", 30), S("Chiên", "Chiên bột trên chảo gang đến vàng hai mặt, đập trứng phủ lên.", 10), S("Hoàn thiện", "Rắc hành lá, dùng với đu đủ chua và nước tương pha.", 5)]),

        R("pha-lau-bo", "Phá lấu bò", "mon-ham", "Lòng bò hầm mềm trong nước dừa, ngũ vị và quế hồi, dùng với bánh mì.", 60, 120, 6, RecipeDifficulty.Hard, N(520, 31, 28, 31, 3, 1050),
            [I("Lòng bò hỗn hợp", 1.2m, "kg"), I("Nước dừa", 1, "lít"), I("Nước cốt dừa", 300, "ml"), I("Sả", 5, "cây"), I("Gừng", 80, "g"), I("Ngũ vị hương", 2, "thìa cà phê"), I("Quế", 1, "thanh"), I("Hoa hồi", 3, "cánh")],
            [S("Làm sạch", "Chần lòng với gừng và rượu, cạo rửa kỹ rồi để ráo.", 35), S("Ướp", "Ướp lòng với ngũ vị, tỏi, nước mắm và đường.", 45), S("Hầm", "Xào lòng săn, thêm nước dừa, sả, quế hồi và hầm đến mềm.", 100), S("Hoàn thiện", "Thêm nước cốt dừa, nấu sánh nhẹ và dùng với bánh mì.", 10)]),

        R("chan-ga-sa-tac", "Chân gà sả tắc", "mon-an-vat", "Chân gà giòn ngâm nước mắm chua ngọt với sả, tắc, lá chanh và ớt.", 40, 20, 6, RecipeDifficulty.Medium, N(230, 20, 18, 9, 2, 980),
            [I("Chân gà", 1, "kg"), I("Sả", 8, "cây"), I("Tắc", 12, "quả"), I("Lá chanh", 12, "lá"), I("Ớt", 5, "quả"), I("Gừng", 40, "g"), I("Nước mắm", 5, "muỗng canh"), I("Đường", 6, "muỗng canh")],
            [S("Sơ chế", "Cắt móng, rửa chân gà với muối và gừng.", 15), S("Luộc", "Luộc chân gà vừa chín, ngâm ngay vào nước đá rồi để ráo.", 15), S("Nấu nước ngâm", "Đun nước mắm, đường và nước cho tan, để nguội hoàn toàn.", 8), S("Ngâm", "Trộn chân gà với sả, tắc, lá chanh, ớt và nước mắm; làm lạnh qua đêm.", 480)]),

        R("trung-cut-lon-xao-me", "Trứng cút lộn xào me", "mon-an-vat", "Trứng cút lộn áo sốt me chua ngọt, thêm rau răm và đậu phộng rang.", 20, 15, 4, RecipeDifficulty.Easy, N(310, 19, 26, 15, 3, 760),
            [I("Trứng cút lộn", 24, "quả"), I("Me chín", 100, "g"), I("Đường", 3, "muỗng canh"), I("Nước mắm", 2, "muỗng canh"), I("Tỏi", 4, "tép"), I("Rau răm", 60, "g"), I("Đậu phộng", 70, "g"), I("Hành phi", 40, "g")],
            [S("Luộc trứng", "Luộc trứng cút lộn chín, ngâm mát rồi bóc nhẹ vỏ.", 12), S("Lọc me", "Dầm me với nước nóng và lọc lấy nước cốt.", 5), S("Nấu sốt", "Phi tỏi, thêm nước me, đường, nước mắm và nấu đến sánh.", 6), S("Xào trứng", "Cho trứng vào đảo nhẹ cho bám sốt, thêm rau răm, đậu phộng và hành phi.", 4)]),

        R("lau-mam", "Lẩu mắm", "mon-lau", "Lẩu mắm miền Tây đậm đà với cá, tôm, mực, thịt ba chỉ và rổ rau đồng phong phú.", 50, 60, 6, RecipeDifficulty.Hard, N(620, 44, 55, 25, 9, 1350),
            [I("Mắm cá linh", 250, "g"), I("Cá basa", 500, "g"), I("Tôm", 400, "g"), I("Mực", 350, "g"), I("Thịt ba chỉ", 350, "g"), I("Cà tím", 2, "quả"), I("Sả", 6, "cây"), I("Bông súng và rau đắng", 700, "g")],
            [S("Lọc mắm", "Nấu mắm với nước đến rã rồi lọc kỹ bỏ xương.", 20), S("Nấu nền lẩu", "Xào sả, thêm nước mắm lọc, nước dừa và cà tím, nêm vừa đậm.", 25), S("Chuẩn bị nguyên liệu", "Làm sạch cá, tôm, mực; thái ba chỉ và rửa rau.", 25), S("Dùng lẩu", "Đun sôi nước lẩu, cho thịt và hải sản chín từng lượt, ăn với bún và rau.", 15)]),

        R("lau-ga-la-e", "Lẩu gà lá é", "mon-lau", "Lẩu gà Phú Yên thơm lá é và ớt xiêm xanh, nước dùng ngọt thanh, cay ấm.", 35, 50, 6, RecipeDifficulty.Medium, N(510, 40, 45, 19, 6, 980),
            [I("Gà ta", 1.5m, "kg"), I("Lá é", 250, "g"), I("Ớt xiêm xanh", 12, "quả"), I("Măng tươi", 400, "g"), I("Nấm bào ngư", 300, "g"), I("Sả", 4, "cây"), I("Nước dừa", 1, "lít"), I("Bún tươi", 1, "kg")],
            [S("Ướp gà", "Chặt gà, ướp với lá é giã, ớt xiêm, hành và nước mắm.", 25), S("Xào gà", "Phi sả, xào gà săn để khóa vị ngọt.", 8), S("Nấu lẩu", "Thêm nước dừa và nước, nấu đến gà mềm rồi cho măng, nấm.", 35), S("Hoàn thiện", "Nêm vị, vò nhẹ lá é còn lại cho vào khi ăn cùng bún.", 5)]),

        R("lau-rieu-cua-bap-bo", "Lẩu riêu cua bắp bò", "mon-lau", "Nước lẩu riêu cua chua dịu, ăn cùng bắp bò thái mỏng, đậu hũ và rau nhúng.", 45, 45, 6, RecipeDifficulty.Hard, N(560, 39, 48, 23, 7, 1120),
            [I("Cua đồng xay", 700, "g"), I("Bắp bò", 600, "g"), I("Đậu hũ", 4, "miếng"), I("Cà chua", 5, "quả"), I("Giấm bỗng", 150, "ml"), I("Mắm tôm", 1, "muỗng canh"), I("Hành phi", 50, "g"), I("Rau muống và hoa chuối", 700, "g")],
            [S("Nấu riêu", "Lọc cua với nước, đun lửa vừa để riêu nổi và vớt riêng.", 20), S("Nấu nước lẩu", "Xào cà chua, gạch cua rồi cho vào nước cua cùng giấm bỗng.", 15), S("Chuẩn bị", "Thái bắp bò thật mỏng, chiên đậu hũ và rửa rau nhúng.", 20), S("Dùng lẩu", "Cho riêu và đậu vào nồi, nhúng bò vừa tái và ăn cùng bún, rau.", 10)]),

        R("ca-hap-bia", "Cá hấp bia", "mon-hap", "Cá nguyên con hấp bia với sả, gừng, thì là, giữ thịt cá ngọt và không tanh.", 25, 30, 4, RecipeDifficulty.Medium, N(290, 39, 9, 10, 2, 620),
            [I("Cá chép", 1.3m, "kg"), I("Bia", 500, "ml"), I("Sả", 5, "cây"), I("Gừng", 50, "g"), I("Thì là", 80, "g"), I("Hành lá", 60, "g"), I("Cà chua", 2, "quả"), I("Chanh", 2, "quả")],
            [S("Sơ chế cá", "Làm sạch cá, khứa thân và xát gừng muối để khử tanh.", 15), S("Ướp", "Ướp cá với nước mắm, tiêu và nhồi gừng, thì là vào bụng.", 15), S("Hấp", "Lót sả dưới nồi, đặt cá, rót bia và hấp kín đến chín.", 25), S("Hoàn thiện", "Thêm cà chua, hành và thì là cuối thời gian; dùng với nước mắm gừng.", 5)]),

        R("ga-hap-la-chanh", "Gà hấp lá chanh", "mon-hap", "Gà ta hấp nguyên con với sả, gừng và lá chanh, da vàng, thịt mọng ngọt.", 30, 45, 6, RecipeDifficulty.Medium, N(400, 43, 7, 22, 2, 650),
            [I("Gà ta", 1.6m, "kg"), I("Lá chanh", 15, "lá"), I("Sả", 6, "cây"), I("Gừng", 60, "g"), I("Hành tím", 5, "củ"), I("Muối hột", 500, "g"), I("Chanh", 2, "quả"), I("Rau răm", 80, "g")],
            [S("Sơ chế", "Chà gà với gừng và muối, rửa sạch rồi thấm khô.", 15), S("Ướp", "Xoa gà với muối tiêu, nhồi sả, gừng và lá chanh vào bụng.", 30), S("Hấp", "Lót muối hột và sả dưới nồi, đặt gà lên hấp kín đến chín.", 40), S("Hoàn thiện", "Để gà nghỉ, chặt miếng và dùng với muối tiêu chanh, rau răm.", 8)]),
    ];
}
