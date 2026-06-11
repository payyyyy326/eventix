"use client";

import { getImageUrl } from "@/shared/lib/image-url";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { authStorage } from "@/shared/lib/auth-storage";

export default function Header() {
    const router = useRouter();
    const user = authStorage.getUser();
    const avatarUrl = getImageUrl(user?.avatarUrl);
    const [open, setOpen] = useState(false);

    function handleLogout() {
        authStorage.clear();
        router.push("/login");
    }

    return (
        <header className="sticky top-0 z-50 border-b border-white/10 bg-slate-950/80 backdrop-blur-xl">
            <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-6">
                <Link href="/" className="text-xl font-bold text-white">
                    Eventix
                </Link>

                <nav className="hidden items-center gap-6 text-sm text-slate-300 md:flex">
                    <Link href="/events" className="hover:text-white">
                        Sự kiện
                    </Link>
                    <Link href="/categories" className="hover:text-white">
                        Danh mục
                    </Link>
                </nav>

                {!user ? (
                    <div className="flex items-center gap-3">
                        <Link
                            href="/login"
                            className="rounded-xl px-4 py-2 text-sm font-medium text-slate-200 hover:bg-white/10"
                        >
                            Đăng nhập
                        </Link>

                        <Link
                            href="/register"
                            className="rounded-xl bg-gradient-to-r from-violet-600 to-blue-600 px-4 py-2 text-sm font-semibold text-white"
                        >
                            Đăng ký
                        </Link>
                    </div>
                ) : (
                    <div className="relative">
                        <button
                            onClick={() => setOpen(!open)}
                            className="flex items-center gap-3 rounded-xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-200 hover:bg-white/10"
                        >
                            {avatarUrl ? (
                                <img
                                    src={avatarUrl}
                                    alt={user.fullName || "Avatar"}
                                    className="h-8 w-8 rounded-full object-cover"
                                />
                            ) : (
                                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-r from-violet-600 to-blue-600 font-bold text-white">
                                    {(user.fullName || user.email)?.charAt(0).toUpperCase()}
                                </div>
                            )}

                            <span className="hidden md:block">
                                {user.fullName || user.email}
                            </span>

                            <span className="text-slate-400">▾</span>
                        </button>

                        {open && (
                            <div className="absolute right-0 mt-3 w-56 rounded-xl border border-white/10 bg-slate-900 p-2 shadow-2xl">
                                <Link
                                    href="/profile"
                                    className="block rounded-lg px-4 py-2 text-sm text-slate-300 hover:bg-white/10 hover:text-white"
                                    onClick={() => setOpen(false)}
                                >
                                    Hồ sơ cá nhân
                                </Link>
                                <Link
                                    href="/change-password"
                                    className="block rounded-lg px-4 py-2 text-sm text-slate-300 hover:bg-white/10 hover:text-white"
                                    onClick={() => setOpen(false)}
                                >
                                    Đổi mật khẩu
                                </Link>

                                <Link
                                    href="/my-tickets"
                                    className="block rounded-lg px-4 py-2 text-sm text-slate-300 hover:bg-white/10 hover:text-white"
                                    onClick={() => setOpen(false)}
                                >
                                    Vé của tôi
                                </Link>

                                <Link
                                    href="/settings"
                                    className="block rounded-lg px-4 py-2 text-sm text-slate-300 hover:bg-white/10 hover:text-white"
                                    onClick={() => setOpen(false)}
                                >
                                    Cài đặt
                                </Link>

                                <div className="my-2 border-t border-white/10" />

                                <button
                                    onClick={handleLogout}
                                    className="w-full rounded-lg px-4 py-2 text-left text-sm text-red-300 hover:bg-red-500/10 hover:text-red-200"
                                >
                                    Đăng xuất
                                </button>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </header>
    );
}