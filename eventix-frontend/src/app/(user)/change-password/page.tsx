import Header from "@/shared/components/layout/Header";
import ChangePasswordForm from "@/features/user/components/ChangePasswordForm";

export default function ChangePasswordPage() {
    return (
        <main className="min-h-screen bg-[radial-gradient(circle_at_top,_#312e81,_#020617_45%,_#000_100%)]">
            <Header />

            <section className="px-4 py-10">
                <ChangePasswordForm />
            </section>
        </main>
    );
}