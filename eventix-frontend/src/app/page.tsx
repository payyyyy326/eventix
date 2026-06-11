// src/app/page.tsx

import Header from "@/shared/components/layout/Header";

export default function HomePage() {
    return (
        <main className="min-h-screen bg-slate-950 text-white">
            <Header />

            <section className="mx-auto max-w-7xl px-6 py-24">
                <h1 className="max-w-3xl text-5xl font-bold">
                    Khám phá và đặt vé sự kiện dễ dàng với Eventix
                </h1>

                <p className="mt-6 max-w-xl text-slate-300">
                    Nền tảng quản lý, bán vé và đặt chỗ sự kiện hiện đại.
                </p>
            </section>
        </main>
    );
}