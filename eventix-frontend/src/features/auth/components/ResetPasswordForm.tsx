"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { authApi } from "../api/auth.api";
import { getErrorMessage } from "@/shared/lib/error";

export default function ResetPasswordForm() {
    const router = useRouter();
    const searchParams = useSearchParams();

    const email = searchParams.get("email") ?? "";

    const [otpCode, setOtpCode] = useState("");

    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");

    const [step, setStep] = useState(1);
    const [loading, setLoading] = useState(false);

    function verifyOtpStep() {
        if (!otpCode.trim()) {
            alert("Vui lòng nhập OTP");
            return;
        }

        setStep(2);
    }

    async function handleResetPassword(
        e: React.FormEvent<HTMLFormElement>
    ) {
        e.preventDefault();

        try {
            setLoading(true);

            const res = await authApi.resetPassword({
                email,
                otpCode,
                newPassword,
                confirmPassword,
            });

            alert(res.message);

            router.push("/login");
        } catch (error) {
            alert(getErrorMessage(error));
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="w-full max-w-md rounded-2xl border border-white/10 bg-white/10 p-8 shadow-2xl backdrop-blur-xl">
            <div className="mb-8 text-center">
                <h1 className="text-3xl font-bold text-white">
                    Đặt lại mật khẩu
                </h1>

                <p className="mt-2 text-sm text-slate-300">
                    {email}
                </p>
            </div>

            {step === 1 && (
                <div className="space-y-5">
                    <input
                        className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-center text-lg tracking-[0.3em] text-white outline-none"
                        placeholder="000000"
                        value={otpCode}
                        onChange={(e) =>
                            setOtpCode(e.target.value)
                        }
                    />

                    <button
                        onClick={verifyOtpStep}
                        className="w-full rounded-xl bg-gradient-to-r from-violet-600 to-blue-600 py-3 font-semibold text-white"
                    >
                        Tiếp tục
                    </button>
                </div>
            )}

            {step === 2 && (
                <form
                    onSubmit={handleResetPassword}
                    className="space-y-5"
                >
                    <input
                        type="password"
                        placeholder="Mật khẩu mới"
                        value={newPassword}
                        onChange={(e) =>
                            setNewPassword(e.target.value)
                        }
                        className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none"
                    />

                    <input
                        type="password"
                        placeholder="Nhập lại mật khẩu"
                        value={confirmPassword}
                        onChange={(e) =>
                            setConfirmPassword(e.target.value)
                        }
                        className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none"
                    />

                    <button
                        disabled={loading}
                        className="w-full rounded-xl bg-gradient-to-r from-violet-600 to-blue-600 py-3 font-semibold text-white"
                    >
                        {loading
                            ? "Đang xử lý..."
                            : "Đặt lại mật khẩu"}
                    </button>
                </form>
            )}
        </div>
    );
}