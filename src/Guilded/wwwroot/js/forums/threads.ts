let $editPostModal: JQuery;
let $createReply: JQuery;

function onPostReplySuccess(data: any) {
    let pageUrl = $("input[type='hidden'].last-page").val();

    window.location.href = pageUrl;
}

function onPostReplyError(response: JQueryXHR) {
    let $updatedForm = $(response.responseText);

    $updatedForm.insertBefore($createReply);
    $createReply.remove();
}

function onPostReplyBegin() {
    $(this).addClass("loading");
}

function insertRequestVerificationTokenIntoData(formData: any) {
    const verificationTokenName = "__RequestVerificationToken";

    let verificationToken = $(`input[name='${verificationTokenName}']`).val();

    formData[verificationTokenName] = verificationToken;
}

function replyFormEntersLoading() {
    $createReply.addClass("loading");
}

function replyFormExitsLoading() {
    $createReply.removeClass("loading");
}

function onBeforeDeleteSend() {
    hideErrorAndSuccessMessages();
    replyFormEntersLoading();
}

function onDeleteError(response: JQueryXHR) {
    showErrorMessage(response.responseText || response.statusText);
}

function onReplyDeleteClick(e: JQueryEventObject) {
    confirmAction("Are you sure you want to delete this reply?", () => {
        let $reply = $(e.target).closest(".comment");
        let replyId = $reply.data("reply-id");
        let formData: any = {
            replyId: replyId,
        };

        insertRequestVerificationTokenIntoData(formData);

        $.ajax({
            url: $("input[type='hidden'].delete-reply-url").val(),
            type: "DELETE",
            data: formData,
            beforeSend: onBeforeDeleteSend,
            complete: replyFormExitsLoading,
            success: () => {
                $reply.remove();
                showSuccessMessage("Successfully deleted the reply.");
            },
            error: onDeleteError
        });
    });
}

function onThreadDeleteClick() {
    confirmAction("Are you sure you want to delete this thread?", () => {
        let formData: any = {
        };

        insertRequestVerificationTokenIntoData(formData);

        $.ajax({
            url: $("input[type='hidden'].delete-thread-url").val(),
            type: "DELETE",
            data: formData,
            beforeSend: onBeforeDeleteSend,
            complete: replyFormExitsLoading,
            success: () => {
                showSuccessMessage("Successfully deleted the thread. You will be redirected to the forums in a moment.");

                setTimeout(() => {
                    let forumUrl: string = $("input[type='hidden'].forum-url").val();

                    window.location.href = forumUrl;
                }, 1500);
            },
            error: onDeleteError
        });
    });
}

function onQuoteClick() {
    alert("A thing");
}

function onEditClick(e: JQueryEventObject) {
    const $post = $(e.target).closest(".comment");
    let editUrl = $post.data("edit-url");

    $editPostModal.find("form")
        .addClass("loading")
        .attr("action", editUrl);

    $editPostModal.modal("show")
        .data("update-element", $post);

    $.ajax({
        method: "GET",
        url: editUrl,
        success: (response) => {
            let mdEditor = MarkdownEditor.getEditor($editPostModal.find(".markdown-editor"));

            mdEditor.text = "" + response;
            $editPostModal.find("form").removeClass("loading");
        },
        error: (response: JQueryXHR) => {
            $editPostModal.modal("hide");
            showErrorMessage(response.responseText || response.statusText);
        }
    });
}

function onEditPostBegin() {
    $editPostModal.find(".ui.warning.segment").addClass("hidden");
    $editPostModal.find("form").addClass("loading")
}

function onEditPostSuccess(response: any) {
    let $postToUpdate = $($editPostModal.data("update-element"));
    $postToUpdate.find(".content .text").html(response);
    $editPostModal.modal("hide");
}

function onEditPostComplete() {
    $editPostModal.find("form").removeClass("loading");
}

function onEditPostError(response: JQueryXHR) {
    $editPostModal.find(".ui.warning.segment")
        .text(response.responseText || response.statusText)
        .removeClass("hidden");
}

function onPinOrLockClick(e: JQueryEventObject) {
    const $hiddenInput = $(e.target).find("input[type='hidden'][data-action-method]");
    const actionUrl: string = $hiddenInput.val();

    let actionData = {};
    insertRequestVerificationTokenIntoData(actionData);

    $.ajax({
        method: $hiddenInput.data("action-method"),
        url: actionUrl,
        data: actionData,
        beforeSend: hideErrorAndSuccessMessages,
        success: () => window.location.reload(true),
        error: () => {
            showErrorMessage("An error occurred with your request. Please try again.");
        }
    });
}

$(document).ready(() => {
    $createReply = $("#create-reply");

    $(".ui.button.locking,.ui.button.pinning")
        .on("click", onPinOrLockClick);

    $(".ui.reply.button").on("mousedown mouseup focus click", () => {
        MarkdownEditor.getEditor($("#create-reply-wrapper .markdown-editor")).focus();
    });

    $(".comment .actions")
        .on("click", ".quote", onQuoteClick)
        .on("click", ".edit", onEditClick);

    $(".comment[data-reply-id] .actions")
        .on("click", ".delete", onReplyDeleteClick);

    $(".comment[data-thread-id]")
        .on("click", ".delete", onThreadDeleteClick);

    $editPostModal = $("#editPostModal").modal({
        onShow: onEditPostBegin,
    });
});