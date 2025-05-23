const express = require("express");
const cors = require("cors");
const sqlite3 = require("sqlite3").verbose();

const app = express();
const PORT = 3000;

app.use(cors());
const db = new sqlite3.Database("factory.db");

// 현재 상태 저장용
let status = {
    temperature: 75.0,
    vibration: 0.8
};

// 상태 갱신 및 DB 저장 (6초마다)
setInterval(() => {
    status.temperature = parseFloat((70 + Math.random() * 10).toFixed(1)); // 70~80
    status.vibration = parseFloat((0.5 + Math.random()).toFixed(2));       // 0.5~1.5

    console.log("상태 갱신:", status);

    const timestamp = new Date().toISOString();
    db.run(
        "INSERT INTO SensorLogs (timestamp, vibration, temperature) VALUES (?, ?, ?)",
        [timestamp, status.vibration, status.temperature],
        (err) => {
            if (err) {
                console.error("DB 저장 오류:", err.message);
            } else {
                console.log("DB에 저장됨:", timestamp);
            }
        }
    );
}, 12000);

// Unity 등에서 사용할 API
app.get("/status", (req, res) => {
    res.json(status);
});

app.listen(PORT, () => {
    console.log(`Node.js 서버 실행 중: http://localhost:${PORT}/status`);
});

// 새로 추가된 로그 데이터 API
app.get("/logs", (req, res) => {
    db.all("SELECT * FROM SensorLogs ORDER BY timestamp DESC LIMIT 5", (err, rows) => {
        if (err) {
            res.status(500).json({ error: err.message });
        } else {
            res.json(rows.reverse()); // 최신 → 오래된 순서로 정렬
        }
    });
});
