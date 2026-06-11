import Header from "@/shared/components/layout/Header";
import ProfileForm from "@/features/user/components/ProfileForm";

export default function ProfilePage() {
    return (
        <main className="min-h-screen bg-[radial-gradient(circle_at_top,_#312e81,_#020617_45%,_#000_100%)]">
            <Header />
            <section className="px-4 py-10">
                <ProfileForm />
            </section>
        </main>
    );
}