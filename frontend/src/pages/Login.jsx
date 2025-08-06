import { useState } from "react";
import { post, setToken } from "../api/client";

export default function Login({ onSuccess }) {
  const [email, setEmail] = useState("admin@example.com");
  const [password, setPassword] = useState("password");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const res = await post("/auth/login", { email, password }, { auth: false });
      setToken(res.token);
      onSuccess?.(res);
    } catch (err) {
      setError(err.message || "Login failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} style={{ maxWidth: 360, margin: "64px auto" }}>
      <h2>Sign in</h2>
      <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email" />
      <input
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        type="password"
        placeholder="Password"
      />
      <button disabled={loading}>{loading ? "Signing in..." : "Sign in"}</button>
      {error && <p style={{ color: "crimson" }}>{error}</p>}
    </form>
  );
}
